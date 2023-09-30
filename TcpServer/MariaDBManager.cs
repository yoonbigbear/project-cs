using Microsoft.EntityFrameworkCore;
using MySqlConnector;

public static class MariaDBManager
{
	/*
	 * MySqlConnector는 MySql이나 MariaDB의 ADO.NET. 그 외 아마존 오로라 애저 구글클라우드 페르소나 등등 사용 가능.
	 * 가능한 Async 기능을 사용하는것이 좋다.
	 */

	/*
	 * 문서는 MySql에 있는 문서를 확인해야할듯함.
	 * Connection pooling은 기본적으로 활성화 되어있음.
	 * MySqlConnection을 dispose할때 기본 연결은 활성상태를 유지하는 방식으로 작동. 이후에 새 connection을 열려도 시도할 경우 connection pool에서 만들어진다.
	 * pool을 사용하기 위해선 수동으로 생성하면 적용되지 않는다. 대신 연결 문자열을 인수로 받는 오버로드된 메서드를 사용하면 자동으로 open close dispose를 한다.
	 * 또한 MySqlHelper클래스를 이용하여 정적메서드를 사용해도 된다.
	 * 
	 * 사용하지 않는 connection은 3분이 경과됐을경우 자동으로 연결 제거하고 리소스 확보.
	 * 
	 * Statement는 쿼리의 구문 분석을 한번만 진행하기 때문에 두 번 이살 싱핼되는 문에 대해 직접 실행보다 빠르다.
	 * 
	 * SP는 여러 어플리케이션에서 일관성이 좋고 보안성도 강화된다.
	 * 
	 * TINYINT(1) 대신 BOOL을 사용하도록 한다. (어차피 마샬링 해야하므로)
	 * 1 byte 정수가 필요한 경우는 TINYINT TINYINT UNSIGNED 를 사용
	 * 
	 * float 역시 하용하지 않는것이 좋다. MySql에서는 float을 32bit single precesion 으로 IEEE 754값으로 저장
	 * 하지만 클라이언트로 반환할때는 정밀도를 손실하게된다. 정확도가 중요할 경우 DOUBLE을 사용하거나
	 * (SELECT value+0)같은 방식으로 두 배의 정밀도를 강제로 지정해서 사용.(???)
	 * https://stackoverflow.com/questions/60070417/mysql-8-should-i-be-able-to-write-a-valid-ieee-754-floating-point-number-and-re/60084985#60084985
	 * 
	 * 동기식 메서드는 스레드풀에 악영향을 미칠 수있다. 따라서 가능한 비동기 메서드를 사용한다.
	 * 
	 * 
	 */
	public static void Start()
	{
		using var connection = new MySqlConnection(
			@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		connection.Open();

		using var command = new MySqlCommand("Select field from table;", connection);
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			Console.WriteLine(reader.GetString(0));
		}
	}

	public static async void Read()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		await connection.OpenAsync();

		Console.WriteLine("DBConnected");

		using (var cmd = new MySqlCommand())
		{
			cmd.Connection = connection;
			cmd.CommandText = "INSERT INTO data (some_field) VALUES (@p)";
			cmd.Parameters.AddWithValue("p", "Hello world");
			await cmd.ExecuteNonQueryAsync();
		}

		using (var cmd = new MySqlCommand("SELECT some_field FROM data", connection)) 
		{ 
			using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
				Console.WriteLine(reader.GetString(0));
			}
        }
	}

	public static async void Statement()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		await connection.OpenAsync();

		try
		{
			var command = new MySqlCommand("INSERT INTO myTable VALUES(NULL, @number, @text);", connection);
			await command.PrepareAsync();
			command.Parameters.AddWithValue("@number", 1);
			command.Parameters.AddWithValue("@text", "One");

			for (int i = 1; i <= 1000; i++)
			{
				command.Parameters["@number"].Value = i;
				command.Parameters["@text"].Value = "A string value";

				await command.ExecuteNonQueryAsync();
			}
		}
		catch(MySqlException ex) 
		{
			Console.WriteLine($"Error {ex.Number} has occurred: {ex.Message}");
		}
	}

	public static async void CreateStoredProcedure()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		await connection.OpenAsync();
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		{
			string spname = "add_emp";
			cmd.CommandText = $"DROP PROCEDURE IF EXISTS {spname}";
			await cmd.ExecuteNonQueryAsync();
		}

		{
			string tableName = "emp";
			cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
			await cmd.ExecuteNonQueryAsync();
		}

		{
			cmd.CommandText = @"CREATE TABLE emp (
							empno INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
							first_name VARCHAR(20),
							last_name VARCHAR(20),
							birthdate DATE)";
			await cmd.ExecuteNonQueryAsync();
		}

		{
			cmd.CommandText = @"CREATE PROCEDURE add_emp(
							IN fname VARCHAR(20),
							IN lname VARCHAR(20),
							IN bday DATETIME,
							OUT empno INT)
							BEGIN
								INSERT INTO emp(first_name, last_name, birstdate)
										VALUES(fname, lname, DATE(bday));
								SET empno = LAST_INSERT_ID();
							END";
			await cmd.ExecuteNonQueryAsync();
		}
	}

	public static async void AccessSP()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		await connection.OpenAsync();
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		cmd.CommandText = "add_emp";
		cmd.CommandType = System.Data.CommandType.StoredProcedure;

		cmd.Parameters.AddWithValue("@lname", "Jones");
		cmd.Parameters["@lname"].Direction = System.Data.ParameterDirection.Input;

		cmd.Parameters.AddWithValue("@fname", "Tom");
		cmd.Parameters["@fname"].Direction = System.Data.ParameterDirection.Input;

		cmd.Parameters.AddWithValue("@bday", "1940-06-07");
		cmd.Parameters["@bday"].Direction = System.Data.ParameterDirection.Input;

		cmd.Parameters.Add("@empno", MySqlDbType.Int32);
		cmd.Parameters["@emppno"].Direction = System.Data.ParameterDirection.Output;

		await cmd.ExecuteNonQueryAsync();

		Console.WriteLine($"Employee number: {cmd.Parameters["@empno"].Value}");
		Console.WriteLine($"Birthday: {cmd.Parameters["@bday"].Value}");
	}

	public static async void Bulk()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		var bl = new MySqlBulkLoader( connection );
		bl.Local = true;
		bl.TableName = "Career";
		bl.FieldTerminator = "\t";
		bl.LineTerminator = "\n";
		bl.FileName = "..\\career.txt";
		bl.NumberOfLinesToSkip = 3; // 파일을 읽어서 하는 경우 첫 라인들을 스킵할 수 있다.

		try
		{
			Console.WriteLine("Connecting to MySQL...");
			await connection.OpenAsync();

			int count = await bl.LoadAsync();
			Console.WriteLine($"{count} lines uploaded");


			string sql = "SELECT Name, Age, Profession FROM Career";
			var cmd = new MySqlCommand(sql, connection);
			MySqlDataReader rdr = await cmd.ExecuteReaderAsync();

			while (await rdr.ReadAsync())
			{
				Console.WriteLine($"{rdr[0]} -- {rdr[1]} -- {rdr[2]}");
			}

			rdr.Close();

			connection.Close();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
		Console.WriteLine("Done.");
	}

}

/*
 * EFCore는 Polmelo.EntityFrameworkCore.MySql 사용
 */

class Program
{
	static void SubMain()
	{
		using (var context = new BlogDataContext())
		{
			var john = new Author { Name = "John T Author", Email = "john@example.com" };
			context.Authors.Add(john);

			var jane = new Author { Name = "Jane Q Hacker", Email = "jane@example.com" };
			context.Authors.Add(jane);

			var post = new Post { Title = "Hello World", Content = "I wrote an app using EF Core!", Author = jane };
			context.Posts.Add(post);

			post = new Post { Title = "How to use EF Core", Content = "It's pretty easy", Author = john };
			context.Posts.Add(post);

			context.SaveChanges();
		}

		using(var context = new BlogDataContext())
		{
			var posts = context.Posts
				.Include(p => p.Author)
				.ToList();

			foreach (var post in posts)
			{
				Console.WriteLine($"{post.Title} by {post.Author.Name}");
			}
		}
	}
}
public class BlogDataContext : DbContext
{
	static readonly string connectionString = @"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account";

	public DbSet<Author> Authors { get; set; }
	public DbSet<Post> Posts { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
	}
}

public class Post
{
	public int PostId { get; set; }
	public string Title { get; set; }
	public string Content { get; set; }
	public Author Author { get; set; }
}

public class Author
{
	public int AuthorId { get; set; }
	public string Name { get; set; }
	public string Email { get; set; }
	public List<Post> Posts { get; set; }
}