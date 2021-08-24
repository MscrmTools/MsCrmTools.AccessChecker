using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MsCrmTools.AccessChecker
{
    public class Permission
    {
        public string Name { get; set; }

        public AccessRights AccessRights { get; set; }

        public PermissionType PermissionType { get; set; }

        public string BUName { get; set; }

        public Guid SharedRecordId { get; set; }
        public string SharedRecordTable { get; set; }

        public string SharedRecordUrl { get; set; }

    }

    public enum PermissionType
    {
        Role,
        TeamRole,
        UserRole,
        UserShared,
        TeamShared,
        UserRelated,
        TeamRelated,
        Shared,
        Heirarchy
    }

    public enum AccessCheck
    {
        Default,
        User,
        Team
    }
    public class Privilege
    {
        private PrivDef PrivDef;

        public Privilege(PrivDef privDef)
        {
            this.PrivDef = privDef;
        }

        public bool HasAccess { get; set; }

        public Guid PrivilegeId { get; set; }

        public string PrivilegeName { get; set; }

        public string PrivilegeType { get { return PrivDef.Type; } }
        public string Label { get { return PrivDef.Label; } }

        public bool StartsWith(string Name)
        {
            return Name.StartsWith(PrivilegeType);
        }

        public AccessRights AccessRight { get { return PrivDef.AccessRight; } }

        public List<Permission> Permissions { get; set; } = new List<Permission>();
    }

    internal class PrivilegeSet
    {
        public List<Privilege> Privileges { get; set; }
    }

    public class PrivDef
    {
        public PrivDef(string type, string label, AccessRights accessRight)
        {
            Type = type;
            AccessRight = accessRight;
            Label = label;
        }

        public string Type { get; private set; }
        public AccessRights AccessRight { get; private set; }
        public string Label { get; private set; }
    }

    public class User
    {
        public Guid Id { get; set; }
        public Guid BUId { get; set; }

        public string Name { get; set; }

        public List<Team> Teams { get; set; }
    }

    public class Team
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    internal static class Helper
    {
        static readonly PrivDef[] Types = new PrivDef[] { new PrivDef("prvcreate","CreateAccess", AccessRights.CreateAccess),
            new PrivDef("prvread","ReadAccess", AccessRights.ReadAccess),new PrivDef( "prvwrite","WriteAccess", AccessRights.WriteAccess),
            new PrivDef("prvdelete","DeleteAccess", AccessRights.DeleteAccess),new PrivDef( "prvappend","AppendAccess", AccessRights.AppendAccess),
            new PrivDef("prvassign","AssignAccess", AccessRights.AssignAccess), new PrivDef( "prvappendto","AppendToAccess", AccessRights.AppendToAccess),
            new PrivDef( "prvshare","ShareAccess", AccessRights.ShareAccess) };

        public static PrivilegeSet NewPrivilegeSet()
        {
            var returnPS = new PrivilegeSet();

            returnPS.Privileges = Types.Select(accType => new Privilege(accType)).ToList();// { PrivilegeType = accType.Type, AccessRight = accType.AccessRight }).ToList();

            return returnPS;
        }

        public static List<Privilege> NewPrivList()
        {

            return Types.Select(privDef => new Privilege(privDef)).ToList();
        }


        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static Control FindByTag(Control root, string tag)
        {
            if (root == null)
            {
                return null;
            }

            if (root.Tag is string && (string)root.Tag == tag)
            {
                return root;
            }

            return (from Control control in root.Controls
                    select FindByTag(control, tag)).FirstOrDefault(c => c != null);
        }

        public static Control FindByTagEndsWith(Control root, string tag)
        {
            if (root == null)
            {
                return null;
            }

            if (root.Tag is string && ((string)root.Tag).EndsWith(tag))
            {
                return root;
            }

            return (from Control control in root.Controls
                    select FindByTagEndsWith(control, tag)).FirstOrDefault(c => c != null);
        }
    }


}
