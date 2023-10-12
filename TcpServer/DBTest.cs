using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using NetCore;
using System.Data;
using System.Xml.Linq;

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
DROP PROCEDURE IF EXISTS insert_table_account;
CREATE PROCEDURE insert_table_account(
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
			cmd.CommandText = "insert_table_account";
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
				return result;
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
DROP PROCEDURE IF EXISTS insert_table_characters;
CREATE PROCEDURE insert_table_characters(
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
			cmd.CommandText = "insert_table_characters";
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
				return result;
			}
		}
	}

	public static async void CreateBagTableAndProcedure()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=game");
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		//테이블 생성 프로시져
		{
			await connection.OpenAsync();

			cmd.CommandText = @"
							DROP TABLE IF EXISTS bags;
							CREATE TABLE bags (
							id BIGINT NOT NULL PRIMARY KEY,
							tid INT UNSIGNED NOT NULL,
							charid BIGINT NOT NULL,
							count INT NOT NULL);
							CREATE index idx_bags_char on bags(charid);
							CREATE index idx_bags_char_tid on bags(charid, tid);
							CREATE index idx_bags_tid on bags(tid);";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
							DROP PROCEDURE IF EXISTS insert_bags_items;
							CREATE PROCEDURE insert_bags_items(
							IN in_id BIGINT,
							IN in_tid INT,
							IN in_charid BIGINT,
							IN in_count INT
							)								
							BEGIN
								INSERT INTO characters(bags.id, bags.tid, bags.charid, bags.count) 
									VALUE(in_id, in_tid, in_charid, in_count);
							END";
			await cmd.ExecuteNonQueryAsync();
		}
	}

	public static async Task<long> InsertOrUpdateBulkItem(List<ItemDB> items)
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=game;
			AllowLoadLocalInfile=true;");
		var table = new DataTable();
		table.Columns.AddRange(new[]
		{ new DataColumn("id", typeof(long)),
		  new DataColumn("tid", typeof(uint)),
		  new DataColumn("charid", typeof(long)),
		  new DataColumn("count", typeof(uint))
		});

		try
		{
			await connection.OpenAsync();

			//bulk insert

			foreach (var e in items)
			{
				var row = table.NewRow();
				row.SetField(0, e.dbid);
				row.SetField(1, e.tid);
				row.SetField(2, e.charid);
				row.SetField(3, e.count);
				table.Rows.Add(row);
			}

			var bulkCopy = new MySqlBulkCopy(connection);
			bulkCopy.DestinationTableName = "bags";
			var result = await bulkCopy.WriteToServerAsync(table);

		}
		catch(Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
		return 0;
	}

	public static async void CreateGuildTableAndProcedure()
	{
		await using var connection = new MySqlConnection(@"Address=127.0.0.1; Port=3400;
			Username=root; Password=admin;
			Database=game");
		var cmd = new MySqlCommand();
		cmd.Connection = connection;

		//테이블 생성 프로시져
		{
			await connection.OpenAsync();

			cmd.CommandText = @"
							DROP TABLE IF EXISTS guild;
							CREATE TABLE guild (
							uid INT NOT NULL PRIMARY KEY,
							name VARCHAR(20) NOT NULL UNIQUE KEY,
							charid BIGINT NOT NULL,
							count INT NOT NULL,
							register_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);
							CREATE index idx_guild_char on guild(charid);
							CREATE index idx_guild_name on guild(name);";
			await cmd.ExecuteNonQueryAsync();

			//길드 추가.
			cmd.CommandText = @"							
DROP PROCEDURE IF EXISTS insert_guild_table;
CREATE PROCEDURE insert_guild_table(
IN in_uid INT,
IN in_name VARCHAR(20),
IN in_charid BIGINT,
IN in_count INT
)								
BEGIN
INSERT INTO guild(`uid`, `name`, `charid`, `count`) VALUE(in_uid, in_name, in_charid, in_count);
END";
			await cmd.ExecuteNonQueryAsync();

			//길드 업데이트
			cmd.CommandText = @"							
DROP PROCEDURE IF EXISTS update_guild_table;
CREATE PROCEDURE update_guild_table(
IN in_uid INT,
IN in_name VARCHAR(20),
IN in_charid BIGINT,
IN in_count INT
)								
BEGIN
UPDATE guild SET `name` = in_name, `charid` = in_charid, `count`= in_count where `uid` = in_uid;
END";
			await cmd.ExecuteNonQueryAsync();

			//길드 제거
			cmd.CommandText = @"							
DROP PROCEDURE IF EXISTS delete_guild_table;
CREATE PROCEDURE delete_guild_table(
IN in_uid INT
)								
BEGIN
DELETE FROM guild where `uid` = in_uid;
END";
			await cmd.ExecuteNonQueryAsync();
		}
	}
}
