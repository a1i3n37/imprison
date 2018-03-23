using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;

namespace Imprison
{
    internal class LocalUserManager
    {
        private readonly PrincipalContext _principalContext;

        public LocalUserManager()
        {
            _principalContext = new PrincipalContext(ContextType.Machine);
        }

        ~LocalUserManager()
        {
        }

        public UserPrincipal GetUser(string username)
        {
            return UserPrincipal.FindByIdentity(_principalContext, username);
        }

        public GroupPrincipal GetGroup(string groupname)
        {
            return GroupPrincipal.FindByIdentity(_principalContext, groupname);
        }

        public bool UserExists(string username)
        {
            var user = GetUser(username);
            return user != null;
        }

        public bool GroupExists(string groupname)
        {
            var group = GetGroup(groupname);
            return group != null;
        }

        public UserPrincipal CreateUser(string username, string password)
        {
            var user = GetUser(username);

            if (user != null)
                return user;

            user = new UserPrincipal(_principalContext, username, password, true);
            user.Name = username;
            user.SamAccountName = username;
            user.Save();

            return user;
        }

        public bool DeleteUser(string username)
        {
            var user = GetUser(username);

            if (user == null)
                return false;

            user.Delete();
            return true;
        }

        public bool AddUserToGroup(string username, string groupname)
        {
            var user = GetUser(username);
            var group = GetGroup(groupname);

            if (user == null || group == null)
                return false;

            group.Members.Add(user);
            group.Save();

            return true;
        }

        public bool IsUserInGroup(string username, string groupname)
        {
            var user = GetUser(username);
            var group = GetGroup(groupname);

            if (user == null || group == null)
                return false;

            return group.Members.Contains(user);
        }
    }
}
