using Microsoft.Data.SqlClient;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string GetConnectionString()
{
    return app.Configuration.GetConnectionString("StudentDB")
        ?? throw new InvalidOperationException(
            "StudentDB connection string was not found.");
}

app.MapGet("/", async () =>
{
    var rows = new StringBuilder();

    await using var connection =
        new SqlConnection(GetConnectionString());

    await connection.OpenAsync();

    const string sql =
        "SELECT Id, Name, Email, Course FROM Students ORDER BY Id";

    await using var command =
        new SqlCommand(sql, connection);

    await using var reader =
        await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        rows.Append($"""
            <tr>
                <td>{reader.GetInt32(0)}</td>
                <td>{WebUtility.HtmlEncode(reader.GetString(1))}</td>
                <td>{WebUtility.HtmlEncode(reader.GetString(2))}</td>
                <td>{WebUtility.HtmlEncode(reader.GetString(3))}</td>
            </tr>
            """);
    }

    var html = $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <title>Student Database</title>
            <style>
                body {
                    font-family: Arial;
                    background: #eef6fc;
                    margin: 40px;
                }

                .container {
                    max-width: 850px;
                    margin: auto;
                    background: white;
                    padding: 30px;
                    border-radius: 12px;
                    box-shadow: 0 4px 15px #aaa;
                }

                h1 {
                    color: #0078d4;
                    text-align: center;
                }

                form {
                    display: grid;
                    gap: 10px;
                    margin-bottom: 25px;
                }

                input, button {
                    padding: 10px;
                    font-size: 16px;
                }

                button {
                    background: #0078d4;
                    color: white;
                    border: none;
                    cursor: pointer;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                }

                th, td {
                    border: 1px solid #ccc;
                    padding: 10px;
                    text-align: left;
                }

                th {
                    background: #0078d4;
                    color: white;
                }
            </style>
        </head>

        <body>
            <div class="container">
                <h1>Azure Student Record System</h1>

                <form method="post" action="/add">
                    <input name="name" placeholder="Student name" required>
                    <input name="email" type="email"
                           placeholder="Email address" required>
                    <input name="course" placeholder="Course" required>
                    <button type="submit">Add Student</button>
                </form>

                <table>
                    <tr>
                        <th>ID</th>
                        <th>Name</th>
                        <th>Email</th>
                        <th>Course</th>
                    </tr>
                    {{rows}}
                </table>
            </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
});

app.MapPost("/add", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    string name = form["name"].ToString();
    string email = form["email"].ToString();
    string course = form["course"].ToString();

    await using var connection =
        new SqlConnection(GetConnectionString());

    await connection.OpenAsync();

    const string sql = """
        INSERT INTO Students (Name, Email, Course)
        VALUES (@Name, @Email, @Course)
        """;

    await using var command =
        new SqlCommand(sql, connection);

    command.Parameters.AddWithValue("@Name", name);
    command.Parameters.AddWithValue("@Email", email);
    command.Parameters.AddWithValue("@Course", course);

    await command.ExecuteNonQueryAsync();

    return Results.Redirect("/");
});

app.Run();
