using MySqlConnector;
using NetCore;

public class DBTest
{
	public static async void CreateAccountTableAndProcedure()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		//테이블 생성 프로시져
		{
			await connection.OpenAsync();

			cmd.CommandText = @"
							DROP TABLE IF EXISTS account;
							CREATE TABLE account (
							id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
							name VARCHAR(20) NOT NULL,
							register_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);
							CREATE index idx_account_name on account(name);";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
DROP PROCEDURE IF EXISTS create_table_account;
CREATE PROCEDURE create_table_account(
IN in_name VARCHAR(20), OUT out_id INT
)								
BEGIN
IF NOT EXISTS (SELECT 1 FROM account WHERE account.name = in_name) THEN
	INSERT INTO account(account.name) VALUE(in_name);
	SET out_id = LAST_INSERT_ID();
END IF;
END";
			await cmd.ExecuteNonQueryAsync();
		}
	}

	public static async Task<int> CreateAccount(string name)
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
			if (result == 1)
			{
				return (int)cmd.Parameters["out_id"].Value;
			}
			else
			{
				return -1;
			}
		}
	}

	public static async void CreateCharacterTableAndProcedure()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		//테이블 생성 프로시져
		{
			await connection.OpenAsync();

			cmd.CommandText = @"
							DROP TABLE IF EXISTS characters;
							CREATE TABLE characters (
							id BIGINT NOT NULL PRIMARY KEY,
							name VARCHAR(20) NOT NULL,
							register_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);
							CREATE index idx_characters_name on characters(name);";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
DROP PROCEDURE IF EXISTS create_table_characters;
CREATE PROCEDURE create_table_characters(
IN in_id BIGINT,
IN in_name VARCHAR(20)
)								
BEGIN
IF NOT EXISTS (SELECT 1 FROM characters WHERE characters.name = in_name) THEN
	INSERT INTO characters(characters.id, characters.name) VALUE(in_id, in_name);
END IF;
END";
			await cmd.ExecuteNonQueryAsync();
		}
	}

	public static async Task<long> CreateCharacter(string name)
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=account");
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		{
			await connection.OpenAsync();
			cmd.CommandText = "create_table_characters";
			cmd.CommandType = System.Data.CommandType.StoredProcedure;

			var guid = IdGen.GenerateGUID(IdGen.IdType.Player, 1);
			cmd.Parameters.AddWithValue("in_id", guid);
			cmd.Parameters["in_id"].Direction = System.Data.ParameterDirection.Input;

			cmd.Parameters.AddWithValue("in_name", name);
			cmd.Parameters["in_name"].Direction = System.Data.ParameterDirection.Input;

			var result = await cmd.ExecuteNonQueryAsync();
			if (result == 1)
			{
				return guid;
			}
			else
			{
				return 0;
			}
		}
	}

}
