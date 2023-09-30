using MySqlConnector;

public class DBTest
{
	public static async void CreateTableandSP()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		//테이블 생성 프로시져
		{
			await connection.OpenAsync();

			string spName = "create_table_account";
			cmd.CommandText = $"DROP PROCEDURE IF EXISTS {spName}";
			await cmd.ExecuteNonQueryAsync();

			string tableName = "account";
			cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"CREATE TABLE account (
							id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
							name VARCHAR(20))";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
DROP PROCEDURE IF EXISTS create_table_account;
CREATE PROCEDURE create_table_account(
IN in_name VARCHAR(20), OUT out_id INT
)								
BEGIN
IF NOT EXISTS (SELECT 1 FROM account WHERE account.name = in_name) THEN
	INSERT INTO account(account.name) VALUE(in_name);
END IF;
END";
			await cmd.ExecuteNonQueryAsync();
		}
	}

	public static async void CallSP(string name)
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		{
			await connection.OpenAsync();
			cmd.CommandText = "create_table_account";
			cmd.CommandType = System.Data.CommandType.StoredProcedure;

			cmd.Parameters.AddWithValue("in_name", name);
			cmd.Parameters["in_name"].Direction = System.Data.ParameterDirection.Input;

			cmd.Parameters.Add("out_id", MySqlDbType.Int32);
			cmd.Parameters["out_id"].Direction = System.Data.ParameterDirection.Output;

			var result = await cmd.ExecuteNonQueryAsync();
			var id = ((int)result);
		}
	}

}
