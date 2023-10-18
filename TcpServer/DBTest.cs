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
		catch (Exception ex)
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

			//길드 테이블 생성
			cmd.CommandText = @"
DROP TABLE IF EXISTS guild;
CREATE TABLE guild (
uid INT NOT NULL PRIMARY KEY,
name VARCHAR(20) NOT NULL UNIQUE KEY,
charid BIGINT NOT NULL,
count INT NOT NULL,
register_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE index idx_guild_char on guild(charid);
CREATE index idx_guild_name on guild(name);
";
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



			//길드멤버 테이블 생성
			cmd.CommandText = @"
DROP TABLE IF EXISTS guild_member;
CREATE TABLE guild_member 
(
`charid` BIGINT UNSIGNED NOT NULL DEFAULT '0' COMMENT 'identifier',
`uid` INT UNSIGNED NOT NULL DEFAULT '0' COMMENT 'Character Global Unique ID',
`register_date` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
PRIMARY KEY(`charid`),
KEY (`uid`)
);
";
			await cmd.ExecuteNonQueryAsync();

			//길드원 추가
			cmd.CommandText = @"
DROP PROCEDURE IF EXISTS insert_guild_member;
CREATE PROCEDURE insert_guild_member(
IN `in_charid` BIGINT,
IN `in_uid` INT
)								
BEGIN

INSERT INTO guild_member(`charid`, `uid`) VALUE (`in_charid`,`in_uid`);

END";
			await cmd.ExecuteNonQueryAsync();

			//길드원 탈퇴
			cmd.CommandText = @"
DROP PROCEDURE IF EXISTS delete_guild_member;
CREATE PROCEDURE delete_guild_member(
IN `in_charid` BIGINT
)								
BEGIN

DELETE FROM guild_member where `charid` = `in_charid`;

END";
			await cmd.ExecuteNonQueryAsync();

			//길드 해체
			cmd.CommandText = @"
DROP PROCEDURE IF EXISTS delete_all_guild_member;
CREATE PROCEDURE delete_all_guild_member(
IN `in_uid` INT
)								
BEGIN

DELETE FROM guild_member where `uid` = `in_uid`;

END";
			await cmd.ExecuteNonQueryAsync();
		}
	}

	public static async void CreateMailDatabaseProcedure()
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
DROP TABLE IF EXISTS mail;
CREATE TABLE mail (
`guid` BIGINT UNSIGNED NOT NULL DEFAULT '0' COMMENT 'Identifier',
`type` TINYINT unsigned NOT NULL DEFAULT '0',
`sender` bigint unsigned NOT NULL default '0' COMMENT 'Character Global Unique ID',
`receiver` bigint unsigned NOT NULL default '0' COMMENT 'Character Global Unique ID',
`subject` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
`body` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
`has_items` tinyint unsigned NOT NULL default '0',
`expire_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
`deliver_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
`money` bigint unsigned NOT NULL DEFAULT '0',
`checked` tinyint unsigned NOT NULL DEFAULT '0',
PRIMARY KEY (`guid`),
KEY `idx_receiver` (`receiver`)
)";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
DROP TABLE IF EXISTS `mail_items`;
CREATE TABLE `mail_items` (
`mail_id`		bigint unsigned NOT NULL DEFAULT '0',
`item_guid`		bigint unsigned NOT NULL DEFAULT '0',
`receiver`		bigint unsigned NOT NULL DEFAULT '0' COMMENT 'Characet Global Unique Id',
PRIMARY KEY			(`item_guid`),
KEY `idx_receiver`	(`receiver`),
KEY `idx_mail_id`	(`mail_id`)
)";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
-- receiver id를 받아서 현재 메일함에 있는 모든 메일을 받아옵니다.

DROP PROCEDURE IF EXISTS select_all_received_mails;
CREATE PROCEDURE select_all_received_mails(
IN `in_receiver` BIGINT
)								
BEGIN
SELECT * FROM mail where mail.receiver = in_receiver;
END";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
-- mail id를 기반으로 선택한 메일 1개와 동봉된 아이템 목록을 받아옵니다.

DROP PROCEDURE IF EXISTS select_read_received_mails;
CREATE PROCEDURE select_read_received_mails(
IN `in_mail_id` BIGINT
)
BEGIN
SELECT * FROM mail, mail_items where mail.mail_id = in_mail_id and mail_items.mail_id = in_mail_id;
UPDATE mail SET `checked` = 1 WHERE `mail_id` = `in_mail_id`;
-- exprie_time도 변경해줘야 한다.
END";
			await cmd.ExecuteNonQueryAsync();

			cmd.CommandText = @"
-- 기간이 지난 메일은 지웁니다.

DROP PROCEDURE IF EXISTS delete_expired_mails;
CREATE PROCEDURE delete_expired_mails(
IN `in_receiver` BIGINT
)
BEGIN
DELETE FROM mail_items WHERE mail_items.receiver IN 
(SELECT guid FROM mail where mail.expire_time < CURRENT_TIMESTAMP AND mail.receiver = `in_receiver`);
DELETE FROM mail WHERE mail.expire_time < CURRENT_TIMESTAMP AND mail.receiver = `in_receiver`;
END";
			await cmd.ExecuteNonQueryAsync();
		}
	}
}
