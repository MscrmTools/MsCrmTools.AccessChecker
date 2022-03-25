using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using MsCrmTools.AccessChecker.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace MsCrmTools.AccessChecker
{
    public partial class AccessChecker : PluginControlBase, IGitHubPlugin, IHelpPlugin, IPayPalPlugin
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of class <see cref="AccessChecker"/>
        /// </summary>
        public AccessChecker()
        {
            InitializeComponent();
            ai = new AppInsights(aiEndpoint, aiKey, Assembly.GetExecutingAssembly());
            ai.WriteEvent("Control Loaded");
        }

        #endregion Constructor

        #region Interfaces implementation

        private const string aiEndpoint = "https://dc.services.visualstudio.com/v2/track";

        private const string aiKey = "8cf73e9a-d3e1-4e60-be09-87ca27d4a20f";

        private AppInsights ai;

        public string DonationDescription => "Donation for Access Checker";

        public string EmailAccount => "tanguy92@hotmail.com";

        public string HelpUrl
        {
            get
            {
                return "https://github.com/MscrmTools/MsCrmTools.AccessChecker/wiki";
            }
        }

        public string RepositoryName
        {
            get
            {
                return "MsCrmTools.AccessChecker";
            }
        }

        public string UserName
        {
            get
            {
                return "MscrmTools";
            }
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            ExecuteMethod(ProcessRetrieveEntities);
        }

        #endregion Interfaces implementation

        #region Methods

        private void BrowseUser()
        {
            var form = new CrmUserPickerForm(new CrmAccess(Service, ConnectionDetail));

            if (form.ShowDialog() == DialogResult.OK)
            {
                textBox_UserID.Text = form.SelectedUser.Name;
                textBox_UserID.Tag = form.SelectedUser;
                //foreach (Guid userId in form.SelectedUsers.Keys)
                //{
                //    textBox_UserID.Text = form.SelectedUsers[userId];
                //    textBox_UserID.Tag = userId;
                //}
            }
            else
            {
                textBox_UserID.Text = string.Empty;
                textBox_UserID.Tag = null;
            }
        }

        private void BtnBrowseClick(object sender, EventArgs e)
        {
            ExecuteMethod(BrowseUser);
        }

        private void BtnRetrieveEntitiesClick(object sender, EventArgs e)
        {
            ExecuteMethod(ProcessRetrieveEntities);
        }

        private void BtnRetrieveRightsClick(object sender, EventArgs e)
        {
            ExecuteMethod(RetrieveAccessRights);
        }

        private void BtnSearchRecordIdClick(object sender, EventArgs e)
        {
            if (cBoxEntities.SelectedIndex < 0)
            {
                MessageBox.Show(ParentForm, "Please select an entity in the list before using the search action",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lp = new LookupSingle(((EntityInfo)cBoxEntities.SelectedItem).Metadata, Service);
            lp.StartPosition = FormStartPosition.CenterParent;
            if (lp.ShowDialog() == DialogResult.OK)
            {
                txtObjectId.Text = lp.SelectedRecordId.ToString("B");
                textBox_PrimaryAttribute.Text = lp.SelectedRecordName;
            }
        }

        private void cbbEntity_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Draw the default background
            e.DrawBackground();
            e.DrawFocusRectangle();

            if (e.Index == -1) return;

            // The ComboBox is bound to a DataTable,
            // so the items are DataRowView objects.
            var attr = (EntityInfo)((ComboBox)sender).Items[e.Index];

            // Retrieve the value of each column.
            string displayName = attr.DisplayName;
            string logicalName = attr.LogicalName;

            // Get the bounds for the first column
            Rectangle r1 = e.Bounds;
            r1.Width /= 2;

            // Draw the text on the first column
            using (SolidBrush sb = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(displayName, e.Font, sb, r1);
            }

            // Get the bounds for the second column
            Rectangle r2 = e.Bounds;
            r2.X = e.Bounds.Width / 2;
            r2.Width /= 2;

            // Draw the text on the second column
            using (SolidBrush sb = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(logicalName, e.Font, sb, r2);
            }
        }

        private void CBoxEntitiesSelectedIndexChanged(object sender, EventArgs e)
        {
            btnSearchRecordId.Enabled = cBoxEntities.SelectedItem != null;
        }

        private void ProcessRetrieveEntities()
        {
            cBoxEntities.Items.Clear();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Retrieving Tables...",
                AsyncArgument = null,
                Work = (bw, e) =>
                {
                    EntityQueryExpression entityQueryExpression = new EntityQueryExpression
                    {
                        Properties = new MetadataPropertiesExpression
                        {
                            AllProperties = false,
                            PropertyNames = { "DisplayName", "LogicalName", "Attributes", "ObjectTypeCode", "PrimaryIdAttribute", "PrimaryNameAttribute" }
                        },
                        AttributeQuery = new AttributeQueryExpression
                        {
                            // Récupération de l'attribut spécifié
                            Properties = new MetadataPropertiesExpression
                            {
                                AllProperties = false,
                                PropertyNames = { "DisplayName", "SchemaName", "LogicalName", "EntityLogicalName", "OptionSet" }
                            }
                        }
                    };

                    RetrieveMetadataChangesRequest retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest
                    {
                        Query = entityQueryExpression,
                        ClientVersionStamp = null
                    };
                    e.Result = ((RetrieveMetadataChangesResponse)Service.Execute(retrieveMetadataChangesRequest)).EntityMetadata;
                },
                PostWorkCallBack = e =>
                {
                    if (e.Error != null)
                    {
                        MessageBox.Show(this, "An error occured: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        var emds = (EntityMetadataCollection)e.Result;

                        foreach (var emd in emds)
                        {
                            cBoxEntities.Items.Add(new EntityInfo(emd));
                        }

                        cBoxEntities.DrawMode = DrawMode.OwnerDrawFixed;
                        cBoxEntities.DrawItem += cbbEntity_DrawItem;

                        cBoxEntities.SelectedIndex = 0;
                    }
                },
                ProgressChanged = e => { SetWorkingMessage(e.UserState.ToString()); }
            });
        }

        private void ResetPermissions(SplitterPanel splitPanel)
        {
            foreach (Panel panel in splitPanel.Controls.OfType<Panel>())
            {
                panel.BackColor = Color.DarkGray;
                TextBox privLabel = (TextBox)Helper.FindByTagEndsWith(panel, "Label");
                privLabel.BackColor = Color.DarkGray;
                privLabel.Text = "";
                var flow = Helper.FindByTagEndsWith(panel, "Flow") as FlowLayoutPanel;

                if (flow != null)
                {
                    flow.Controls.Clear();
                }
            }
        }

        private void ResetPermissions()
        {
            ResetPermissions(splitAccess.Panel1);
            ResetPermissions(splitAccess.Panel2);
        }

        private void RetrieveAccessRights()
        {
            if (cBoxEntities.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Please select an table", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtObjectId.Text.Length == 0)
            {
                MessageBox.Show(this, "Please specify an object Id", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (textBox_UserID.Text.Length == 0)
            {
                MessageBox.Show(this, "Please select a user", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var g = new Guid(txtObjectId.Text);
            }
            catch
            {
                MessageBox.Show(this, "The object ID is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ResetPermissions();

            var parameters = new List<object> {txtObjectId.Text, textBox_UserID.Tag,
                (EntityInfo)cBoxEntities.SelectedItem };

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Retrieving access rights...",
                AsyncArgument = parameters,
                Work = (bw, e) =>
                {
                    var crmAccess = new CrmAccess(Service, ConnectionDetail);
                    var inParameters = (List<object>)e.Argument;
                    var recordId = new Guid(inParameters[0].ToString());
                    var user = (User)inParameters[1];
                    var entityInfo = (EntityInfo)inParameters[2];
                    var entityName = entityInfo.LogicalName;
                    var primaryAttribute = entityInfo.PrimaryAttribute;
                    var result = new CheckAccessResult();

                    if (ConnectionDetail.NewAuthType != Microsoft.Xrm.Tooling.Connector.AuthenticationType.OAuth
                    && ConnectionDetail.NewAuthType != Microsoft.Xrm.Tooling.Connector.AuthenticationType.Certificate)
                    {
                        RetrievePrincipalAccessResponse response = crmAccess.RetrieveRights(
                       user.Id,
                       recordId,
                       entityName);

                        Dictionary<string, Guid> privileges = crmAccess.RetrievePrivileges(entityName);

                        // Get primary attribute value for the current record
                        Entity de = crmAccess.RetrieveDynamicWithPrimaryAttr(recordId, entityName, primaryAttribute);
                        result.RecordName = de.GetAttributeValue<string>(primaryAttribute);

                        // Check Privileges
                        foreach (string privlegeName in privileges.Keys)
                        {
                            if (privlegeName.StartsWith("prvappendto"))
                            {
                                result.HasAppendToAccess = (response.AccessRights & AccessRights.AppendToAccess) == AccessRights.AppendToAccess;
                                result.AppendToPrivilegeId = privileges[privlegeName];
                            }
                            else if (privlegeName.StartsWith("prvappend"))
                            {
                                result.HasAppendAccess = (response.AccessRights & AccessRights.AppendAccess) == AccessRights.AppendAccess;
                                result.AppendPrivilegeId = privileges[privlegeName];
                            }
                            else if (privlegeName.StartsWith("prvassign"))
                            {
                                result.HasAssignAccess = (response.AccessRights & AccessRights.AssignAccess) == AccessRights.AssignAccess;
                                result.AssignPrivilegeId = privileges[privlegeName];
                            }
                            else if (privlegeName.StartsWith("prvcreate"))
                            {
                                result.HasCreateAccess = (response.AccessRights & AccessRights.CreateAccess) == AccessRights.CreateAccess;
                                result.CreatePrivilegeId = privileges[privlegeName];
                            }
                            else if (privlegeName.StartsWith("prvdelete"))
                            {
                                result.HasDeleteAccess = (response.AccessRights & AccessRights.DeleteAccess) == AccessRights.DeleteAccess;
                                result.DeletePrivilegeId = privileges[privlegeName];
                            }
                            else if (privlegeName.StartsWith("prvread"))
                            {
                                result.HasReadAccess = (response.AccessRights & AccessRights.ReadAccess) == AccessRights.ReadAccess;
                                result.ReadPrivilegeId = privileges[privlegeName];
                            }
                            else if (privlegeName.StartsWith("prvshare"))
                            {
                                result.HasShareAccess = (response.AccessRights & AccessRights.ShareAccess) == AccessRights.ShareAccess;
                                result.SharePrivilegeId = privileges[privlegeName];
                            }
                            else if (privlegeName.StartsWith("prvwrite"))
                            {
                                result.HasWriteAccess = (response.AccessRights & AccessRights.WriteAccess) == AccessRights.WriteAccess;
                                result.WritePrivilegeId = privileges[privlegeName];
                            }

                            e.Result = result;
                        }
                    }
                    else
                    {
                        List<Privilege> privList = crmAccess.RetrieveRightsAPI(user.Id, recordId, entityName);

                        if (privList.Any(priv => priv.Permissions.Any(per => per.PermissionType == PermissionType.Role))) crmAccess.GetRoleDetail(privList, user, recordId, entityInfo);

                        if (privList.Any(priv => priv.Permissions.Any(per => per.PermissionType == PermissionType.Shared))) crmAccess.GetShareDetail(privList, user, recordId, entityInfo);

                        e.Result = privList;
                    }
                },
                PostWorkCallBack = e =>
                {
                    if (e.Error != null)
                    {
                        MessageBox.Show(this, "An error occured: " + e.Error.Message, "Error", MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                    }
                    else
                    {
                        if (e.Result is List<Privilege> privList)
                        {
                            var greenColor = Color.LightGreen;// Color.FromArgb(0, 158, 73);
                            var redColor = Color.LightPink;// Color.FromArgb(232, 17, 35);

                            foreach (Privilege privilege in privList)
                            {
                                Control panel = Helper.FindByTag(splitAccess, privilege.PrivilegeType);
                                panel.BackColor = privilege.HasAccess ? greenColor : redColor;
                                var privLabel = (TextBox)Helper.FindByTag(panel, privilege.PrivilegeType + "Label");
                                privLabel.BackColor = panel.BackColor;
                                privLabel.Text = privilege.PrivilegeId.ToString();
                                FlowLayoutPanel flow = Helper.FindByTag(panel, privilege.PrivilegeType + "Flow") as FlowLayoutPanel;
                                //flow.Controls.Clear();
                                if (flow != null)
                                {
                                    foreach (var permission in privilege.Permissions.Where(per => per.PermissionType != PermissionType.Role && per.PermissionType != PermissionType.Shared))
                                    {
                                        flow.Controls.Add(new DisplayAccess(permission));
                                    }
                                }
                            }
                        }
                        else
                        {
                            var result = (CheckAccessResult)e.Result;

                            var greenColor = Color.FromArgb(0, 158, 73);
                            var redColor = Color.FromArgb(232, 17, 35);

                            textBox_PrimaryAttribute.Text = result.RecordName;

                            pnlAppend.BackColor = result.HasAppendAccess
                                                      ? greenColor
                                                      : result.AppendPrivilegeId != Guid.Empty ? redColor : Color.DarkGray;
                            pnlAppendTo.BackColor = result.HasAppendToAccess
                                                        ? greenColor
                                                        : result.AppendToPrivilegeId != Guid.Empty ? redColor : Color.DarkGray;
                            pnlAssign.BackColor = result.HasAssignAccess
                                                      ? greenColor
                                                      : result.AssignPrivilegeId != Guid.Empty ? redColor : Color.DarkGray;
                            pnlCreate.BackColor = result.HasCreateAccess
                                                      ? greenColor
                                                      : result.CreatePrivilegeId != Guid.Empty ? redColor : Color.DarkGray;
                            pnlDelete.BackColor = result.HasDeleteAccess
                                                      ? greenColor
                                                      : result.DeletePrivilegeId != Guid.Empty ? redColor : Color.DarkGray;
                            pnlRead.BackColor = result.HasReadAccess
                                                    ? greenColor
                                                    : result.ReadPrivilegeId != Guid.Empty ? redColor : Color.DarkGray;
                            pnlShare.BackColor = result.HasShareAccess
                                                     ? greenColor
                                                     : result.SharePrivilegeId != Guid.Empty ? redColor : Color.DarkGray;
                            pnlWrite.BackColor = result.HasWriteAccess
                                                     ? greenColor
                                                     : result.WritePrivilegeId != Guid.Empty ? redColor : Color.DarkGray;

                            txtAppendPriv.BackColor = pnlAppend.BackColor;
                            txtAppendPriv.Text = result.AppendPrivilegeId == Guid.Empty
                                                          ? "Privilege does not exist for this entity"
                                                          : result.AppendPrivilegeId.ToString();
                            txtAppendToPriv.BackColor = pnlAppendTo.BackColor;
                            txtAppendToPriv.Text = result.AppendToPrivilegeId == Guid.Empty
                                                            ? "Privilege does not exist for this entity"
                                                            : result.AppendToPrivilegeId.ToString();
                            txtAssignPriv.BackColor = pnlAssign.BackColor;
                            txtAssignPriv.Text = result.AssignPrivilegeId == Guid.Empty
                                                          ? "Privilege does not exist for this entity"
                                                          : result.AssignPrivilegeId.ToString();
                            txtCreatePriv.BackColor = pnlCreate.BackColor;
                            txtCreatePriv.Text = result.CreatePrivilegeId == Guid.Empty
                                                          ? "Privilege does not exist for this entity"
                                                          : result.CreatePrivilegeId.ToString();
                            txtDeletePriv.BackColor = pnlDelete.BackColor;
                            txtDeletePriv.Text = result.DeletePrivilegeId == Guid.Empty
                                                          ? "Privilege does not exist for this entity"
                                                          : result.DeletePrivilegeId.ToString();
                            txtReadPriv.BackColor = pnlRead.BackColor;
                            txtReadPriv.Text = result.ReadPrivilegeId == Guid.Empty
                                                        ? "Privilege does not exist for this entity"
                                                        : result.ReadPrivilegeId.ToString();
                            txtSharePriv.BackColor = pnlShare.BackColor;
                            txtSharePriv.Text = result.SharePrivilegeId == Guid.Empty
                                                         ? "Privilege does not exist for this entity"
                                                         : result.SharePrivilegeId.ToString();
                            txtWritePriv.BackColor = pnlWrite.BackColor;
                            txtWritePriv.Text = result.WritePrivilegeId == Guid.Empty
                                                         ? "Privilege does not exist for this entity"
                                                         : result.WritePrivilegeId.ToString();
                        }

                        ai.WriteEvent("User Checked", 1);
                    }
                }
            });
        }

        private void TsbCloseClick(object sender, System.EventArgs e)
        {
            CloseTool();
        }

        #endregion Methods
    }
}