using System;
using NUnit.Framework;
using Npgsql;
using System.Data;

namespace Rooletochka {
    [TestFixture]
    public class TableTest {
        // Table users has id, name, password fields.
        //
        Table users;
        string name;
        string password;

        void Initialize() {
            string connect = @"Server = 127.0.0.1; Port = 5432; User Id = s;
                Database = practice;";
            var connection = new NpgsqlConnection(connect);
            if(connection.State == ConnectionState.Closed) {
              connection.Open();
            }
            users = new Table("users", connection);
        }

        [Test]
        public void TestInsertAndSelect() {
            Initialize();
            name = "Vasya";
            password = "nagibator228";
            string insert = String.Format("'{0}', '{1}'", name, password);
            users.Insert(insert, "name, password").All();
            users = users.Select("*").Order("-id").All();

            Assert.IsTrue(users.data.Read(), "String was inserted");
            Assert.AreEqual(users.data.GetString(1), name, "Name is the same");
            Assert.AreEqual(users.data.GetString(2), password,
                "Password is the same");
        }

        [Test]
        public void TestUpdate() {
            Initialize();
            string newName = "Vasisualii";
            string setNewName = String.Format("name = '{0}'", newName);
            string setOldName = String.Format("name = '{0}'", name);
            users.Update(setNewName).Where(setOldName).All();
            Table newUsers = users.Select("name").Where(setNewName).All();
            if (newUsers.data.Read()) {
                Assert.AreEqual(newUsers.data.GetString(0), newName);
            }

            // Turn name back.
            //
            users.Update(setOldName).Where(setNewName).All();
            newUsers = users.Select("name").Where(setOldName).All();
            if (newUsers.data.Read()) {
                Assert.AreEqual(newUsers.data.GetString(0), name);
            }
        }
    }
}
