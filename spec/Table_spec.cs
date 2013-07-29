using NSpec;
using System;
using Rooletochka;
using Npgsql;
using System.Data;

class describe_table : nspec
{
    Table users;
    string name;
    string password;
    int counter;

    void Initialize()
    {
        string connect = "Server=127.0.0.1;Port=5432;User Id=s;Database=practice;";
        var connection = new NpgsqlConnection(connect);
        if(connection.State == ConnectionState.Closed) { connection.Open(); }
        users = new Table("users", connection);
    }

    void BeforeInsert()
    {
        Initialize();
        users.Insert("'mala', 'fia'", "name, password");
        users = users.Select("*").All();
        while(users.data.Read()) {
            name = users.data.GetString(1);
            password = users.data.GetString(2);
        }
    }

    void BeforeWhere()
    {
        Initialize();
        counter = 0;
        users = users.Select("*").Where("id < 3").All();
        while(users.data.Read()) { counter++; }
    }

    void insert_and_select_should_work()
    {
        before = () => BeforeInsert();
        it["When I insert something, it should be on last position"] = () =>
            name.should_be("mala");
        it["When I insert something, it should be on last position"] = () =>
            password.should_be("fia");
    }

    void where_should_work_correct()
    {
        before = () => BeforeWhere();
        it["The count of rows should be less than 3"] = () =>
            (counter < 3).should_be_true;
    }
}
