using FireSharp;
using FireSharp.Config;
using FireSharp.Response;
using Google.Cloud.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sticky_Buddy 
{
    public class fsOps
    {
        //store user info
        public static string user_Id;
        public static string user_Fname;
        public static string user_Lname;
        //used in logging in either username or email
        public static string use_Login;
        public static string user_Email;

        //connecting firestore db
        public static FirestoreDb FSdbcon()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"sticky-buddy-5d661-firebase-adminsdk-dmcfy-e23c572668.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            FirestoreDb db = FirestoreDb.Create("sticky-buddy-5d661");

            return db;
        }

        //generate random key
        public static string GenerateRandomKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            StringBuilder stringBuilder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(chars.Length);
                stringBuilder.Append(chars[index]);
            }

            return stringBuilder.ToString();
        }

        //method to store user credentials

        public async static void user_creds(FirestoreDb db, string email, string username, string fname, string lname, string pass)
        {
            try
            {
                CollectionReference doc = db.Collection("users");

                //save if email is unique
                Dictionary<string, object> data1 = new Dictionary<string, object>()
                {
                    {"email", email},
                    {"username", username},
                    {"firstname", fname },
                    {"lastname", lname},
                    {"password", pass},
                    {"userId", GenerateRandomKey(20)}
                };

                await doc.AddAsync(data1);

                MessageBox.Show("Registration Successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //method to check if email is unique
        public static async Task<bool> unique_email(FirestoreDb db, string email)
        {
            try
            {
                CollectionReference doc = db.Collection("users");

                QuerySnapshot querySnap = await doc.WhereEqualTo("email", email).GetSnapshotAsync();

                if (querySnap.Count > 0)
                {
                    MessageBox.Show("Email Already Exists", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        //method to check if username is unique
        public static async Task<bool> unique_username(FirestoreDb db, string username)
        {
            try
            {
                CollectionReference doc = db.Collection("users");

                QuerySnapshot querySnap = await doc.WhereEqualTo("username", username).GetSnapshotAsync();

                if (querySnap.Count > 0)
                {
                    MessageBox.Show("Username Already Exists", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }


        //method to validate user credentials and get user info
        public static async Task<bool> validate_usercred(FirestoreDb db, string use_login, string pass)
        {
            try
            {
                //access firestore collection
                CollectionReference doc = db.Collection("users");
                QuerySnapshot email_query = null;

                if (use_login.Contains("@gmail.com"))
                {
                    //validate by email
                    email_query = await doc
                        .WhereEqualTo("email", use_login)
                        .WhereEqualTo("password", pass)
                        .GetSnapshotAsync();
                }
                //validate by username
                QuerySnapshot username_query = await doc
                    .WhereEqualTo("username", use_login)
                    .WhereEqualTo("password", pass)
                    .GetSnapshotAsync();

                QuerySnapshot resultQuery = email_query?.Count > 0 ? email_query : username_query;

                if (resultQuery?.Count > 0)
                {
                    foreach (DocumentSnapshot docu in resultQuery)
                    {
                        UserData user = docu.ConvertTo<UserData>();
                        user_Fname = user.firstname;
                        user_Lname = user.lastname;
                        user_Email = user.email;
                        use_Login = email_query?.Count > 0 ? user.email : user.username;
                        //user_Id = user.userId;

                        user_Id = docu.Id;
                    }
                    return true;
                }
                else
                {
                    MessageBox.Show("User doesn't exist!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        //get title for notes_title
        public static string note_title { get; set; }

        //method to store rtb content
        public async static void save_note(FirestoreDb db, Dictionary<string, object> content)
        {
            //string cur_Time = DateTime.Now.ToString("MMddyy HHmmss");
            //int num = 1;
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { "content", content }
            };

            try
            {
                //init users_Id
                string userId = user_Id.ToString();

                if (userId != null)
                {
                    //condition; if document is not exists, use title else title + num (retrieval for checking)
                    DocumentReference doc = db.Collection("users").Document(userId).Collection("notes_history").Document($"{note_title}");

                    // Get the existing data first
                    DocumentSnapshot snapshot = await doc.GetSnapshotAsync();

                    if (snapshot != null && snapshot.Exists)
                    {
                        // Document exists, update the existing data
                        await doc.UpdateAsync(dict);
                    }
                    else
                    {
                        // Document doesn't exist, create a  new one
                        await doc.SetAsync(dict);
                    }
                }
                else
                {
                    MessageBox.Show("User removed!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //initialize listener after login and pass firestoreDb as params
        //store notes_title and note_content in NotesData object

        //public static void notes_Listener(FirestoreDb db)
        //{
        //    ucDashboard uc = new ucDashboard();
        //    try
        //    {
        //        //set document to listen to
        //        CollectionReference colRef = db.Collection("users").Document(user_Id).Collection("notes_history");

        //        FirestoreChangeListener listener = colRef.Listen(snapshot =>
        //        {
        //            foreach (DocumentSnapshot docuSnap in snapshot.Documents)
        //            {
        //                foreach (var docuChange in snapshot.Changes)
        //                {
        //                    if (docuChange.ChangeType == (DocumentChange.Type.Added | DocumentChange.Type.Modified))
        //                    {
        //                        //retrieve document data
        //                        //store in retrieved data in dictionary
        //                        Dictionary<string, object> data = docuSnap.ToDictionary();

        //                        uc.Invoke(new Action(() =>
        //                        {
        //                            ucDashboard.note_Title = docuSnap.Id;
        //                            ucDashboard.note_data = data;
        //                        }));
        //                    }
        //                }
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}
    }

    [FirestoreData]
    public class UserData
    {
        [FirestoreProperty]
        public string email { get; set; }
        [FirestoreProperty]
        public string firstname { get; set; }
        [FirestoreProperty]
        public string lastname { get; set; }
        [FirestoreProperty]
        public string password { get; set; }
        [FirestoreProperty]
        public string username { get; set; }
        [FirestoreProperty]
        public string userId { get; set; }
    }
}