using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace MsCrmTools.AccessChecker
{
    /// <summary>
    /// This class provides methods to query CRM Server
    /// </summary>
    public class CrmAccess
    {
        #region Variables

        /// <summary>
        /// CRM proxy data service
        /// </summary>
        private readonly IOrganizationService service;
        private readonly ConnectionDetail connectionDetail;

        #endregion Variables

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the class CrmAccess
        /// </summary>
        /// <param name="service">Organization service</param>
        public CrmAccess(IOrganizationService service, ConnectionDetail connectionDetail)
        {
            this.service = service;
            this.connectionDetail = connectionDetail;
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// Obtains all users corresponding to the search filter
        /// </summary>
        /// <param name="value">Search filter</param>
        /// <returns>List of users matching the search filter</returns>
        public List<Entity> GetUsers(string value)
        {
            
            var users = new List<Entity>();

            var ceStatus = new ConditionExpression("isdisabled", ConditionOperator.Equal, false);

            var feStatus = new FilterExpression();
            feStatus.AddCondition(ceStatus);

            var qe = new QueryExpression("systemuser");
            qe.ColumnSet = new ColumnSet("systemuserid", "fullname", "lastname", "firstname", "domainname", "businessunitid");
            qe.AddOrder("lastname", OrderType.Ascending);
            qe.Criteria = new FilterExpression();
            qe.Criteria.Filters.Add(new FilterExpression());
            qe.Criteria.Filters[0].FilterOperator = LogicalOperator.And;
            qe.Criteria.Filters[0].Filters.Add(feStatus);
            qe.Criteria.Filters[0].Filters.Add(new FilterExpression());
            qe.Criteria.Filters[0].Filters[1].FilterOperator = LogicalOperator.Or;

            if (value.Length > 0)
            {
                bool isGuid = false;

                try
                {
                    Guid g = new Guid(value);
                    isGuid = true;
                }
                catch
                { }

                if (isGuid)
                {
                    var ce = new ConditionExpression("systemuserid", ConditionOperator.Equal, value);
                    qe.Criteria.Filters[0].Filters[1].AddCondition(ce);
                }
                else
                {
                    var ce = new ConditionExpression("fullname", ConditionOperator.Like, $"%{value}%");// value.Replace("*", "%"));
                    var ce2 = new ConditionExpression("firstname", ConditionOperator.Like, $"%{value}%");
                    var ce3 = new ConditionExpression("lastname", ConditionOperator.Like, $"%{value}%");
                    var ce4 = new ConditionExpression("domainname", ConditionOperator.Like, $"%{value}%");

                    qe.Criteria.Filters[0].Filters[1].AddCondition(ce);
                    qe.Criteria.Filters[0].Filters[1].AddCondition(ce2);
                    qe.Criteria.Filters[0].Filters[1].AddCondition(ce3);
                    qe.Criteria.Filters[0].Filters[1].AddCondition(ce4);
                }
            }

            foreach (var record in service.RetrieveMultiple(qe).Entities)
            {
                users.Add(record);
            }

            return users;
        }

        /// <summary>
        /// Retrieve the primary attribute value of the specified object
        /// </summary>
        /// <param name="recordId">Unique identifier of the object</param>
        /// <param name="entityName">Object entity logical name</param>
        /// <param name="primaryAttribute">Entity primary attribute logical name</param>
        /// <returns>Dynamic Entity containing the primary attribute value</returns>
        public Entity RetrieveDynamicWithPrimaryAttr(Guid recordId, string entityName, string primaryAttribute)
        {
            return service.Retrieve(entityName, recordId, new ColumnSet(primaryAttribute));
        }

        /// <summary>
        /// Retrieve the list of all CRM entities
        /// </summary>
        /// <returns>List of all CRM entities</returns>
        public RetrieveAllEntitiesResponse RetrieveEntitiesList()
        {
            var request = new RetrieveAllEntitiesRequest { EntityFilters = EntityFilters.Entity };
            return (RetrieveAllEntitiesResponse)service.Execute(request);
        }

        /// <summary>
        /// Retrieve all privileges definition for the specified entity
        /// </summary>
        /// <param name="entityName">Entity logical name</param>
        /// <returns>List of privileges</returns>
        public Dictionary<string, Guid> RetrievePrivileges(string entityName)
        {
            var request = new RetrieveEntityRequest { LogicalName = entityName, EntityFilters = EntityFilters.Privileges };
            var response = (RetrieveEntityResponse)service.Execute(request);

            var privileges = new Dictionary<string, Guid>();

            foreach (SecurityPrivilegeMetadata spmd in response.EntityMetadata.Privileges)
            {
                privileges.Add(spmd.Name.ToLower(), spmd.PrivilegeId);
            }

            return privileges;
        }

        public List<Privilege> RetrievePrivs(string entityName)
        {
            var request = new RetrieveEntityRequest { LogicalName = entityName, EntityFilters = EntityFilters.Privileges };
            var response = (RetrieveEntityResponse)service.Execute(request);

            var privList = Helper.NewPrivList();
            foreach (SecurityPrivilegeMetadata spmd in response.EntityMetadata.Privileges)
            {
                privList.First(priv => (priv.PrivilegeType + entityName).ToLower() == spmd.Name.ToLower()).PrivilegeId = spmd.PrivilegeId;
            }
            return privList;
        }


        public List<Privilege> RetrieveRightsAPI(Guid userId, Guid objectId, string entityName)
        {
            List<Privilege> privList = RetrievePrivs(entityName);// Helper.NewPrivList();
            var csc = connectionDetail.GetCrmServiceClient();
            Dictionary<string, List<string>> ODataHeaders = new Dictionary<string, List<string>>() {
                                                                    { "Accept", new List<string>() { "application/json" } },
                                                                    {"OData-MaxVersion", new List<string>(){"4.0"}},
                                                                    {"OData-Version", new List<string>(){"4.0"}}
                                                                    };
            if (csc.IsReady)
            {
                 string queryString = $@"systemusers({userId})/Microsoft.Dynamics.CRM.RetrievePrincipalAccessInfo(ObjectId={objectId},EntityName='{entityName}')";
                HttpResponseMessage response = csc.ExecuteCrmWebRequest(HttpMethod.Get, queryString, string.Empty, ODataHeaders, "application/json");

                if (response.IsSuccessStatusCode)
                {
                    PopulateRights(response.Content.ReadAsStringAsync().Result, privList, "Default");

                    // var accountUri = response.Headers.GetValues("OData-EntityId").FirstOrDefault();
                    //Console.WriteLine("Account URI: {0}", accountUri);
                }
                else
                {
                    Console.WriteLine(response.ReasonPhrase);
                }

            }
            else
            {
                Console.WriteLine(csc.LastCrmError);
            }
            return privList;
        }

        private void PopulateRights(string result, List<Privilege> privList, string PermissionName, AccessCheck acCheck = AccessCheck.Default)
        {
            // var resp = response.Content.ReadAsStringAsync().Result;
            var respObj = JObject.Parse(result);
            respObj = JObject.Parse(respObj.SelectToken("$.AccessInfo").ToString());

            string RoleAccess = respObj.SelectToken("$.RoleAccessRights").ToString();
            string PoaAccess = respObj.SelectToken("$.PoaAccessRights").ToString();
            string HsmAcess = respObj.SelectToken("$.HsmAccessRights").ToString();
            string Access = respObj.SelectToken("$.GrantedAccessRights").ToString();
            if (Access == "None") return;
            List<string> accessList = Access.Split(new string[] { ", " }, StringSplitOptions.None).ToList();
            foreach (var acc in accessList)
            {
                var priv = privList.First(prv => prv.Label == acc);
                priv.HasAccess = true;
                if (RoleAccess.Contains(acc)) priv.Permissions.Add(new Permission { PermissionType = acCheck == AccessCheck.Default ? PermissionType.Role : PermissionType.TeamRole, Name = PermissionName });
                if (PoaAccess.Contains(acc)) priv.Permissions.Add(new Permission { PermissionType = PermissionType.Shared, Name = PermissionName });
                if (HsmAcess.Contains(acc)) priv.Permissions.Add(new Permission { PermissionType = PermissionType.Heirarchy, Name = PermissionName });
            }
        }

        internal void GetShareDetail(List<Privilege> privList, User user, Guid recordId, EntityInfo entity)
        {
            // Check POA for Teams
            var fetchXml = $@"
<fetch version='1.0' mapping='logical' distinct='true'>
  <entity name='principalobjectaccess'>
    <attribute name='accessrightsmask' />
    <attribute name='inheritedaccessrightsmask' />
    <filter type='and'>
      <condition attribute='objecttypecode' operator='eq' value='{entity.Metadata.ObjectTypeCode}'/>
    </filter>
    <filter type='and'>
      <condition attribute='objectid' operator='eq' value='{recordId}'/>
    </filter>
    <filter type='and'>
      <condition attribute='principaltypecode' operator='eq' value='9'/>
    </filter>
    <link-entity name='team' from='teamid' to='principalid'>
      <attribute name='name'  alias='teamName' />
      <attribute name='teamid' alias='teamId'/>
      <attribute name='businessunitid' alias='teamBUId'/>
      <link-entity name='businessunit' from='businessunitid' to='businessunitid'>
        <attribute name='name' alias='teamBUName' />
      </link-entity>
      <link-entity name='teammembership' from='teamid' to='teamid'>
        <filter type='and'>
          <condition attribute='systemuserid' operator='eq' value='{user.Id}'/>
        </filter>
      </link-entity>
    </link-entity>
  </entity>
</fetch>";
            var request = new FetchExpression(fetchXml);

            var response = service.RetrieveMultiple(request);

            foreach (Entity teamAcc in response.Entities)
            {
                Permission permission = new Permission();
                permission.PermissionType = (teamAcc.GetAttributeValue<int>("accessrightsmask") > 0) ? PermissionType.TeamShared : PermissionType.TeamRelated;
                permission.AccessRights = (AccessRights)((permission.PermissionType == PermissionType.TeamShared)
                                ? teamAcc.GetAttributeValue<int>("accessrightsmask")
                                : teamAcc.GetAttributeValue<int>("inheritedaccessrightsmask"));
                permission.Name = teamAcc.GetAttributeValue<AliasedValue>("teamName").Value.ToString();
                permission.BUName = teamAcc.GetAttributeValue<AliasedValue>("teamBUName").Value.ToString();
                /*
                privSet.Privileges.First(priv => priv.AccessRight = permission.AccessRights)
                if (teamAcc.GetAttributeValue<int>("accessrightsmask") > 0)
                    privilege.Permissions.Add(new Permission { AccessRights = teamAcc[""] })
                */
                if (permission.PermissionType == PermissionType.TeamRelated) GetRelatedRecord(permission, recordId, (Guid) teamAcc.GetAttributeValue<AliasedValue>("teamId").Value, entity.LogicalName);

                privList.Where(pv => (pv.AccessRight & permission.AccessRights) == pv.AccessRight).ForEach(pv => pv.Permissions.Add(permission));

            }

            fetchXml = $@"
                <fetch version='1.0' mapping='logical' distinct='true'>
                  <entity name='principalobjectaccess'>
                    <attribute name='accessrightsmask' />
                    <attribute name='inheritedaccessrightsmask' />
                    <filter type='and'>
                      <condition attribute='objecttypecode' operator='eq' value='{entity.Metadata.ObjectTypeCode}'/>
                    </filter>
                    <filter type='and'>
                      <condition attribute='objectid' operator='eq' value='{recordId}'/>
                        <condition attribute='principalid' operator='eq' value='{user.Id}'/>
                    </filter>
                  </entity>
                </fetch>";
            request = new FetchExpression(fetchXml);
            response = service.RetrieveMultiple(request);
            foreach (Entity userAcc in response.Entities)
            {
                Permission permission = new Permission();
                permission.PermissionType = (userAcc.GetAttributeValue<int>("accessrightsmask") > 0) ? PermissionType.UserShared : PermissionType.UserRelated;
                permission.AccessRights = (AccessRights)((permission.PermissionType == PermissionType.UserShared)
                                ? userAcc.GetAttributeValue<int>("accessrightsmask")
                                : userAcc.GetAttributeValue<int>("inheritedaccessrightsmask"));
                permission.Name = "UserShared";
                if (permission.PermissionType == PermissionType.UserRelated) GetRelatedRecord(permission, recordId, user.Id, entity.LogicalName);
                privList.Where(pv => (pv.AccessRight & permission.AccessRights) == pv.AccessRight).ForEach(pv => pv.Permissions.Add(permission));

            }
            //check roles
        }

        /// <summary>
        /// Retrieve the access rights for the specified user against the specified object
        /// </summary>
        /// <param name="userId">Unique identifier of the user</param>
        /// <param name="objectId">Unique identifier of the object</param>
        /// <param name="entityName">Logical name of the object entity</param>
        /// <returns>List of access rigths</returns>
        public RetrievePrincipalAccessResponse RetrieveRights(Guid userId, Guid objectId, string entityName)
        {
            try
            {
                // Requête d'accès
                var request = new RetrievePrincipalAccessRequest();
                //var request = new RetrieveSharedPrincipalsAndAccessRequest();
                request.Principal = new EntityReference("systemuser", userId);
                // request.Principal = new EntityReference("team", new Guid("1e9be892-4ae6-eb11-bacb-0022489ba30f"));

                request.Target = new EntityReference(entityName, objectId);
                //var response = (RetrieveSharedPrincipalsAndAccessResponse)service.Execute(request);
                return (RetrievePrincipalAccessResponse)service.Execute(request);
            }
            catch (Exception error)
            {
                throw new Exception("Error while checking rigths: " + error.Message);
            }
        }

        public void GetRoleDetail(List<Privilege> privList, User user, Guid recordId, EntityInfo entity)
        {

            ///Check Team access
            ///
            if (user.Teams == null)
            {
                user.Teams = GetTeams(user.Id);
            }

            foreach (var team in user.Teams)
            {
                var csc = connectionDetail.GetCrmServiceClient();
                Dictionary<string, List<string>> ODataHeaders = new Dictionary<string, List<string>>() {
                                                                    { "Accept", new List<string>() { "application/json" } },
                                                                    {"OData-MaxVersion", new List<string>(){"4.0"}},
                                                                    {"OData-Version", new List<string>(){"4.0"}}
                                                                    };
                if (csc.IsReady)
                {
                    string queryString = $@"teams({team.Id})/Microsoft.Dynamics.CRM.RetrievePrincipalAccessInfo(ObjectId={recordId},EntityName='{entity.LogicalName}')";
                    HttpResponseMessage respMsg = csc.ExecuteCrmWebRequest(HttpMethod.Get, queryString, string.Empty, ODataHeaders, "application/json");

                    if (respMsg.IsSuccessStatusCode)
                    {
                        PopulateRights(respMsg.Content.ReadAsStringAsync().Result, privList, team.Name, AccessCheck.Team);
                    }
                }
            }
            /// Check role Access
            string privName = string.Join("", privList.Where(prv => prv.HasAccess).Select(prv => "<value>" + prv.PrivilegeType + entity.LogicalName + "</value>"));
            var fetchXml = $@"<fetch>
  <entity name='privilege' >
    <attribute name='accessright' />
    <attribute name='name' />
    <filter>
      <condition attribute='name' operator='in' >{privName}
      </condition>
    </filter>
    <link-entity name='roleprivileges' from='privilegeid' to='privilegeid' >
      <attribute name='privilegedepthmask' alias='depth' />
      <link-entity name='role' from='parentrootroleid' to='roleid' >
        <attribute name='name' alias='roleName'/>
        <attribute name='businessunitid' />
        <link-entity name='systemuserroles' from='roleid' to='roleid' >
          <filter>
            <condition attribute='systemuserid' operator='eq' value='{user.Id}' />
          </filter>
        </link-entity>
      </link-entity>
    </link-entity>
  </entity>
</fetch>";
            Guid ownerId = service.Retrieve(entity.LogicalName, recordId, new ColumnSet("ownerid")).GetAttributeValue<EntityReference>("ownerid").Id;
            var request = new FetchExpression(fetchXml);

            var response = service.RetrieveMultiple(request);

            foreach (var priv in response.Entities)
            {
                privName = priv.GetAttributeValue<string>("name");
                Privilege privilege = privList.First(prv => (prv.PrivilegeType + entity.LogicalName).ToLower() == privName.ToLower());
                int depth = (int)priv.GetAttributeValue<AliasedValue>("depth").Value;
                if (depth == 1 && ownerId != user.Id)
                { }
                   else privilege.Permissions.Add(new Permission { PermissionType = PermissionType.UserRole, Name = priv.GetAttributeValue<AliasedValue>("roleName").Value.ToString() });



                //     if (priv.GetAttributeValue<int>("privilegedepthmask") == 1) privilege.Permissions
            }
            // throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieve teams for user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>List of team objects</returns>
        private List<Team> GetTeams(Guid userId)
        {
            string fetchXml = $@"<fetch>
  <entity name='teammembership' >
    <attribute name='teamid' />
    <filter>
      <condition attribute='systemuserid' operator='eq' value='{userId}' />
    </filter>
    <link-entity name='team' from='teamid' to='teamid' >
      <attribute name='name' alias='TeamName' />
      <attribute name='businessunitidname' alias='BUName' />
      <attribute name='businessunitid' alias='BUId' />
    </link-entity>
  </entity>
</fetch>";

            List<Team> teams = new List<Team>();
            var request = new FetchExpression(fetchXml);

            var response = service.RetrieveMultiple(request);

            foreach (var priv in response.Entities)
            {
                Team team = new Team();
                team.Id = priv.GetAttributeValue<Guid>("teamid");
                team.Name = priv.GetAttributeValue<AliasedValue>("TeamName").Value.ToString();
                teams.Add(team);
            }

            return teams;
        }

        private void GetRelatedRecord(Permission permission, Guid recordId, Guid principalId, string entityName)
        {
            var csc = connectionDetail.GetCrmServiceClient();
            Dictionary<string, List<string>> ODataHeaders = new Dictionary<string, List<string>>() {
                                                                    { "Accept", new List<string>() { "application/json" } },
                                                                    {"OData-MaxVersion", new List<string>(){"4.0"}},
                                                                    {"OData-Version", new List<string>(){"4.0"}}
                                                                    };
            if (csc.IsReady)
            {
                string queryString = $@"RetrieveAccessOrigin(ObjectId={recordId},LogicalName='{entityName}',PrincipalId={principalId})";

                HttpResponseMessage response = csc.ExecuteCrmWebRequest(HttpMethod.Get, queryString, string.Empty, ODataHeaders, "application/json");

                if (response.IsSuccessStatusCode)
                {
                    var respObj = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    string result = respObj["Response"].ToString();
                    if (result.StartsWith("PrincipalId has "))
                    {
                        string guid = result.Substring(result.Length - 37, 36);

                        string fetchXml = $@"<fetch>
  <entity name='principalobjectaccess' >
    <attribute name='objecttypecodename' />
    <attribute name='accessrightsmask' />
    <attribute name='objectid' />
    <attribute name='objecttypecode' />
        <filter>
            <condition attribute='objectid' operator='eq' value='{guid}' />4
            <condition attribute='principalid' operator='eq' value='{principalId}' />
        </filter>
  </entity>
</fetch>";
                        var request = new FetchExpression(fetchXml);
                        var resp = service.RetrieveMultiple(request);
                        foreach (Entity userAcc in resp.Entities)
                        {
                            permission.SharedRecordId = userAcc.GetAttributeValue<Guid>("objectid");
                            permission.SharedRecordTable = userAcc.GetAttributeValue<string>("objecttypecode");
                            permission.SharedRecordUrl = connectionDetail.WebApplicationUrl + $@"/main.aspx?pagetype=entityrecord&etn={permission.SharedRecordTable}&id={permission.SharedRecordId}";
                        }
                    }
                }
            }
        }

        #endregion Methods
    }
}