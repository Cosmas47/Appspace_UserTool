using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using MahApps.Metro.Controls;
using RestSharp;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace UserTool_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        RestClient client;
        public string token;
        public Dictionary<int, string> networks = new Dictionary<int, string>();
        static List<User> users = new List<User>();
        string response;

        #region UI Elements
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_Next_Click(object sender, RoutedEventArgs e)
        {
            if (GenerateToken())
            {
                HideCanvas(loginCanvas);
                loginCanvas.Visibility = Visibility.Hidden;

                userCanvas.Visibility = Visibility.Visible;
                ShowCanvas(userCanvas);
            }
        }

        private void userField_GotFocus(object sender, RoutedEventArgs e)
        {
            userField.Text = "";
        }

        private void serverField_GotFocus(object sender, RoutedEventArgs e)
        {
            serverField.Text = "https://";
        }

        private void btn_CreateUser_Click(object sender, RoutedEventArgs e)
        {
            CreateUser();
        }

        private void btn_DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            DeleteUser();
        }

        private void btn_RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            HideCanvas(userCanvas);
            userCanvas.Visibility = Visibility.Hidden;

            userEditCanvas.Visibility = Visibility.Visible;
            ShowCanvas(userEditCanvas);
        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            userEditCanvas.Visibility = Visibility.Hidden;
            HideCanvas(userEditCanvas);

            userCanvas.Visibility = Visibility.Visible;
            ShowCanvas(userCanvas);
        }

        private void unameField_GotFocus(object sender, RoutedEventArgs e)
        {
            unameField.Text = "";
            HideResult();
        }

        private void emailField_GotFocus(object sender, RoutedEventArgs e)
        {
            emailField.Text = "";
        }

        private void firstNameField_GotFocus(object sender, RoutedEventArgs e)
        {
            firstNameField.Text = "";
        }

        private void lastNameField_GotFocus(object sender, RoutedEventArgs e)
        {
            lastNameField.Text = "";
        }

        private void btn_UpdateUserPassword_Click(object sender, RoutedEventArgs e)
        {
            UpdateUserPassword();
        }
        #endregion

        #region Animations

        void HideCanvas(Canvas c)
        {
            DoubleAnimation animation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.2));
            c.BeginAnimation(Canvas.OpacityProperty, animation);
        }

        void ShowCanvas(Canvas c)
        {
            DoubleAnimation animation = new DoubleAnimation(2, TimeSpan.FromSeconds(0.2));
            c.BeginAnimation(OpacityProperty, animation);
        }

        async void ShowResult(string text)
        {
            if (userCanvas.Visibility == Visibility.Visible)
            {
                Label l = output;
                l.Content = text;
                DoubleAnimation animation = new DoubleAnimation(2, TimeSpan.FromSeconds(0.3));
                l.BeginAnimation(OpacityProperty, animation);
            }
            else
            {
                Label l = output1;
                l.Content = text;
                DoubleAnimation animation = new DoubleAnimation(2, TimeSpan.FromSeconds(0.3));
                l.BeginAnimation(OpacityProperty, animation);
            }

        }

        void HideResult()
        {
            Label l = output;
            DoubleAnimation animation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
            l.BeginAnimation(OpacityProperty, animation);
            l.Content = "";
        }
        #endregion

        #region Functions and Processing
        bool GenerateToken()
        {
            //Connect to server            
            client = new RestClient(serverField.Text);
            bool connected = false;

            #region Security Token Request
            var request = new RestRequest();
            request.Resource = "api/v1/token/request";
            request.Method = Method.POST;
            request.AddHeader("Content-Type", "application/json"); //All output is formatted to XML in either case
            request.AddHeader("Request.Authentication", "Nexus.AppSpace.Service.Contract.Messages.Authentication");

            RequestString rs = new RequestString();
            AuthStrings au = new AuthStrings();
            au.AccountId = "1";
            au.Password = passField.Password;
            au.Username = userField.Text;
            rs.Authentication = au;

            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddBody(rs);

            IRestResponse response;
            string status = "";
            try
            {
                response = client.Execute(request);
                token = DeserializeXMLResponse(response.Content, "SecurityToken");
                status = DeserializeXMLResponse(response.Content, "Status");
            }
            catch (System.IO.FileNotFoundException e)
            {
                output.Content += e.InnerException.ToString();
            }

            if (status == "Success")
            {
                connected = true;
                FetchNetworks();
                FetchUsers();
            }

            return connected;
            #endregion
        }

        void FetchNetworks()
        {
            try
            {
                #region Fetch Networks List
                networks.Clear();

                var request = new RestRequest();
                request.Resource = "api/v1/core/networks";
                request.Method = Method.GET;
                request.AddHeader("Content-Type", "application/xml"); //All output is formatted to XML in either case
                request.AddHeader("Token", token);
                request.RequestFormat = RestSharp.DataFormat.Xml;

                response = client.Execute(request).Content;

                //output.Text += "Fetching Networks...";

                XmlDocument xDoc = FetchXMLResponse(response);
                XmlNode root = xDoc.LastChild;
                XmlNodeList nodeList = root.LastChild.ChildNodes;
                foreach (XmlNode node in nodeList)
                {
                    //output.Text += node.ChildNodes[4].InnerText.ToString();
                    //output.Text += System.Environment.NewLine;
                    string id = node.ChildNodes[2].InnerText.ToString();
                    string name = node.ChildNodes[4].InnerText.ToString();

                    networks.Add(int.Parse(id), name);
                    networkField.Items.Add(name);
                }

                if (networks.Count > 0)
                {
                    networkField.SelectedIndex = 0;
                }
                #endregion
            }
            catch (Exception e)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETNETWORKLIST_MESSAGE]: " + e.Message + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETNETWORKLIST_STACK]: " + e.StackTrace + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETNETWORKLIST_DATA]: " + e.Data + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETNETWORKLIST_RESPONSE]: " + response + System.Environment.NewLine);
            }
        }

        void FetchUsers()
        {
            try
            {
                #region Fetch Users List
                userList.Items.Clear();
                users.Clear();

                var request = new RestRequest();
                request.Resource = "api/v1/core/users";
                request.Method = Method.GET;
                request.AddHeader("Content-Type", "application/xml"); //All output is formatted to XML in either case
                request.AddHeader("Token", token);
                request.RequestFormat = RestSharp.DataFormat.Xml;

                response = client.Execute(request).Content;

                XmlDocument xDoc = FetchXMLResponse(response);
                XmlNode root = xDoc.LastChild;
                XmlNodeList nodeList = root.LastChild.ChildNodes;

                foreach (XmlNode node in nodeList)
                {
                    User u = new User();
                    foreach (XmlNode n in node.ChildNodes)
                    {
                        if (n.LocalName == "Id")
                            u.Id = int.Parse(n.InnerText);
                        if (n.LocalName == "Username")
                            u.Username = n.InnerText;
                        if (n.LocalName == "Email")
                            u.Email = n.InnerText;
                        if (n.LocalName == "FirstName")
                            u.FirstName = n.InnerText;
                        if (n.LocalName == "Lastname")
                            u.Lastname = n.InnerText;
                        if (n.LocalName == "HomeNetworkId")
                            u.HomeNetworkId = int.Parse(n.InnerText);
                        if (n.LocalName == "Password")
                            u.Password = n.InnerText;
                        if (n.LocalName == "AccountId")
                            u.AccountId = int.Parse(n.InnerText);
                        if (n.LocalName == "MobileNo")
                            u.MobileNo = n.InnerText;
                    }
                    users.Add(u);
                    userList.Items.Add(u.Username);
                }
                userList.SelectedIndex = 0;

                if (networks.Count > 0)
                {
                    networkField.SelectedIndex = 0;
                }
                #endregion
            }
            catch (Exception e)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETUSERSLIST_MESSAGE]: " + e.Message + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETUSERSLIST_STACK]: " + e.StackTrace + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETUSERSLIST_DATA]: " + e.Data + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[GETUSERSLIST_RESPONSE]: " + response + System.Environment.NewLine);
            }
        }

        void CreateUser()
        {
            try
            {
                var request = new RestRequest();
                request.Resource = "api/v1/core/users";
                request.Method = Method.POST;
                request.AddHeader("Content-Type", "application/json"); //All output is formatted to XML in either case
                request.AddHeader("Token", token);
                request.AddHeader("Request.Users", "Nexus.AppSpace.Service.Contract.Messages.AppSpaceIntegration.v1.User.UserRequest");

                UsersList users = new UsersList();
                users.Users = new User[1];

                User user = new User();
                user.Username = unameField.Text;
                user.Email = emailField.Text;
                user.HomeNetworkId = GetNetworkID(networkField.SelectedValue.ToString());
                Console.WriteLine("HomeNetworkID = " + user.HomeNetworkId);
                user.FirstName = firstNameField.Text;
                user.Lastname = lastNameField.Text;

                //Defaults
                user.MobileNo = "2148675309";
                user.SecretQuestion = "q";
                user.SecretAnswer = "a";
                user.Status = 1;
                user.AccountId = 1;
                user.Password = passwordBox.Password;
                user.WorkNo = "2148675309";

                user.RoleIds = new int[1];
                user.RoleIds[0] = 5;

                users.Users[0] = user; //Add this user to the root array

                request.RequestFormat = RestSharp.DataFormat.Json;
                request.AddBody(users);

                response = client.Execute(request).Content;

                string status = DeserializeXMLResponse(response, "Status");
                if (status == "Success")
                    ShowResult("Success - User Created");

                string newUserId = DeserializeXMLResponseItem(response, "Users", "Id");
                UpdateUser(newUserId);

                FetchUsers();
            }
            catch (Exception e)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[CREATEUSER_MESSAGE]: " + e.Message + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[CREATEUSER_STACK]: " + e.StackTrace + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[CREATEUSER_DATA]: " + e.Data + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[CREATEUSER_RESPONSE]: " + response + System.Environment.NewLine);
            }
        }

        void DeleteUser()
        {
            try
            {
                int userID = User.GetUserID(userList.SelectedValue.ToString());

                var request = new RestRequest();
                request.Resource = "api/v1/core/users/" + userID;
                request.Method = Method.DELETE;
                request.AddHeader("Content-Type", "application/json"); //All output is formatted to XML in either case
                request.AddHeader("Token", token);

                request.RequestFormat = RestSharp.DataFormat.Json;

                request.AddBody("");

                response = client.Execute(request).Content;

                string status = DeserializeXMLResponse(response, "Status");
                if (status == "Success")
                    ShowResult("Success - User Deleted");

                FetchUsers();
            }
            catch (Exception e)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[DELETEUSER_MESSAGE]: " + e.Message + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[DELETEUSER_STACK]: " + e.StackTrace + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[DELETEUSER_DATA]: " + e.Data + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[DELETEUSER_RESPONSE]: " + response + System.Environment.NewLine);
            }
        }

        void UpdateUser(string id)
        {
            try
            {
                var request = new RestRequest();
                request.Resource = "api/v1/core/users";
                request.Method = Method.PUT;
                request.AddHeader("Content-Type", "application/json"); //All output is formatted to XML in either case
                request.AddHeader("Token", token);
                request.AddHeader("Request.Users", "Nexus.AppSpace.Service.Contract.Messages.AppSpaceIntegration.v1.User.UserRequest");

                UsersList users = new UsersList();
                users.Users = new User[1];

                User user = new User();
                user.Username = unameField.Text;
                user.Id = int.Parse(id);
                user.HomeNetworkId = GetNetworkID(networkField.SelectedValue.ToString());

                //Defaults
                user.Status = 1;
                user.RoleIds = new int[1];
                user.RoleIds[0] = 5;

                users.Users[0] = user; //Add this user to the root array

                request.RequestFormat = RestSharp.DataFormat.Json;
                request.AddBody(users);

                response = client.Execute(request).Content;

                string status = DeserializeXMLResponse(response, "Status");
                if (status == "Success")
                    ShowResult(status);
            }
            catch (Exception e)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSER_MESSAGE]: " + e.Message + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSER_STACK]: " + e.StackTrace + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSER_DATA]: " + e.Data + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSER_RESPONSE]: " + response + System.Environment.NewLine);
            }
        }

        void UpdateUserPassword()
        {
            try
            {
                User thisUser = User.GetUser(userList.SelectedValue.ToString());

                var request = new RestRequest();
                request.Resource = "api/v1/core/users/" + thisUser.Id;
                request.Method = Method.PUT;
                request.AddHeader("Content-Type", "application/json"); //All output is formatted to XML in either case
                request.AddHeader("Token", token);
                request.AddHeader("Request.User", "Nexus.AppSpace.Service.Contract.Messages.AppSpaceIntegration.v1.User.UserRequest");

                UserList uL = new UserList();

                User user = new User();
                user.Username = thisUser.Username;
                user.Id = thisUser.Id;
                user.Password = passwordBox1.Password;
                user.HomeNetworkId = thisUser.HomeNetworkId;
                user.Email = thisUser.Email;
                user.FirstName = thisUser.FirstName;
                user.Lastname = thisUser.Lastname;
                user.MobileNo = thisUser.MobileNo;
                user.AccountId = thisUser.AccountId;
                user.Status = 1;

                uL.User = user; //Add this user to the root array

                request.RequestFormat = RestSharp.DataFormat.Json;
                request.AddBody(uL);

                response = client.Execute(request).Content;

                string status = DeserializeXMLResponse(response, "Status");
                if (status == "Success")
                    ShowResult("Success - Password Updated");
                else
                    ShowResult("Failure - Check Log");
            }
            catch (Exception e)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSERPASSWORD_MESSAGE]: " + e.Message + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSERPASSWORD_STACK]: " + e.StackTrace + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSERPASSWORD_DATA]: " + e.Data + System.Environment.NewLine);
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " [ERROR].[UPDATEUSERPASSWORD_RESPONSE]: " + response + System.Environment.NewLine);
            }
        }

        string GetResult(string data)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(data);

            XmlNode response = xDoc.FirstChild;
            XmlNode status = response.FirstChild;

            return status.InnerText;
        }

        int GetNetworkID(string networkName)
        {
            int i = 0;
            foreach (KeyValuePair<int, string> item in networks)
            {
                if (item.Value == networkName)
                    i = item.Key;
            }
            return i;
        }

        string DeserializeXMLResponse(string data, string parameter)
        {
            string output = "ERROR";
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(data);

            XmlNodeList state = xDoc.GetElementsByTagName(parameter);
            if (state[0] != null)
                output = state[0].InnerText.ToString();

            return output;
        }

        string DeserializeXMLResponseItem(string data, string parameter, string element)
        {
            string output = "ERROR";
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(data);

            XmlNodeList usersList = xDoc.GetElementsByTagName(parameter);
            XmlNodeList userData = usersList[0].ChildNodes;
            foreach (XmlNode node in userData[0])
            {
                if (node.LocalName == element)
                    output = node.FirstChild.InnerText;
            }
            
            return output;
        }

        XmlDocument FetchXMLResponse(string data)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(data);
            return xDoc;
        }
        #endregion

        struct RequestString
        {
            public AuthStrings Authentication;
        }

        struct AuthStrings
        {
            public string AccountId;
            public string Username;
            public string Password;
        }

        struct UsersList
        {
            public User[] Users;
        }

        struct UserList
        {
            public User User;
        }

        struct User
        {
            public int AccountId;
            public int Id;
            public string CloudGuid;
            public string Email;
            public string FirstName;
            //public int GroupId;
            public int HomeNetworkId;
            public string Lastname;
            public string MobileNo;
            public string Password;
            public int[] RoleIds;
            public string SecretAnswer;
            public string SecretQuestion;
            public int Status;
            public string UserDN;
            public string Username;
            public string WorkNo;

            public static int GetUserID(string name)
            {
                int i = 0;
                foreach (User u in users)
                    if (u.Username == name)
                        i = u.Id;

                return i;
            }

            public static User GetUser(string name)
            {
                User user = new User();

                foreach (User u in users)
                    if (u.Username == name)
                        user = u;

                return user;
            }
        }
    }


}
