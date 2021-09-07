using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MsCrmTools.AccessChecker
{
    public partial class DisplayAccess : UserControl
    {
        public DisplayAccess(Permission permission)
        {
            InitializeComponent();
            switch (permission.PermissionType)
            {
                case PermissionType.Role:
                    break;
                case PermissionType.TeamRole:
                    image.Image = Properties.Resources.Team;
                    lblTitle.Text = "Role from Team: " + permission.Name;
                    AddToolTip("The user has permissions given via a team their are in");

                    break;
                case PermissionType.UserRole:
                    image.Image = Properties.Resources.User;
                    lblTitle.Text = "User Role: " + permission.Name;
                    AddToolTip("The user has permissions given directly from their role");

                    break;
                case PermissionType.UserShared:
                    image.Image = Properties.Resources.ShareUser;
                    lblTitle.Text = "Shared with User";
                    AddToolTip("The record is shared with the user");
                    break;
                case PermissionType.TeamShared:
                    image.Image = Properties.Resources.ShareTeam;
                    lblTitle.Text = "Shared with Team: " + permission.Name;
                    AddToolTip("The record is shared with a team that the user belongs to");
                    break;
                case PermissionType.UserRelated:
                    image.Image = Properties.Resources.ShareUser;
                    lblTitle.Text = "Related record shared";
                    if (permission.SharedRecordId != null)
                    {
                        linkRelated.Text = $@"Shared {permission.SharedRecordTable}: {permission.SharedRecordId}";
                        linkRelated.Tag = permission.SharedRecordUrl;
                        linkRelated.Visible = true;
                        AddToolTip("The record is not directly shared but due to relationships has the permissions of the parent (or higher) which has been shared");

                    }
                    else AddToolTip("The record has got inherited permissions, but the parent record can not be found." + Environment.NewLine + "This is usually caused by historic data not been cleaned up correctly");
                    break;
                case PermissionType.TeamRelated:
                    image.Image = Properties.Resources.ShareTeam;
                    lblTitle.Text = $@"Related Record shared with Team {permission.Name}" ;
                    if (permission.SharedRecordId != null)
                    {
                        linkRelated.Text = $@"Shared {permission.SharedRecordTable}: {permission.SharedRecordId}";
                        linkRelated.Tag = permission.SharedRecordUrl;
                        linkRelated.Visible = true;
                        AddToolTip("The record is not directly shared with the team but due to relationships has the permissions of the parent (or higher) which has been shared");

                    }
                    else AddToolTip("The record has got inherited permissions to a team, but the parent record can not be found." + Environment.NewLine + "This is usually caused by historic data not been cleaned up correctly");

                    break;
                case PermissionType.Shared:
                    break;
                case PermissionType.Heirarchy:
                    break;
                default:
                    image.Visible = false;
                    lblTitle.Text = permission.Name;
                    break;
            }
        }

        private void AddToolTip(string text)
        {
            var toolTip = new ToolTip();
            toolTip.AutomaticDelay = 500;

            toolTip.SetToolTip(this, text);
            toolTip.SetToolTip(lblTitle, text);
            toolTip.SetToolTip(image, text);
            toolTip.SetToolTip(linkRelated, text);



        }

        private void linkRelated_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkRelated.LinkVisited = true;

            // Navigate to a URL.
            System.Diagnostics.Process.Start(linkRelated.Tag.ToString());
        }
    }
}
