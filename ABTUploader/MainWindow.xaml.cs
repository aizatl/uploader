using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Controls;
using System.Data;
using System.Windows.Input;
using static ABTUploader.MainWindow;
using Terminaldiagnostic;
using System.Windows.Documents;
using MySql.Data.MySqlClient;
using Google.Protobuf.WellKnownTypes;
using Mysqlx.Crud;
using System.Windows.Controls.Primitives;

namespace ABTUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void UploadFiles(object sender, RoutedEventArgs e)
        {
            string walletOp = ((ComboBoxItem)walletOperationCombo.SelectedItem).Content.ToString();
            string cardOp = ((ComboBoxItem)cardOperationCombo.SelectedItem).Content.ToString();
            string discountOp = ((ComboBoxItem)discountOperationCombo.SelectedItem).Content.ToString();

            if (string.IsNullOrEmpty(cardFilePath.Text) || string.IsNullOrEmpty(walletFilePath.Text) || string.IsNullOrEmpty(discountFilePath.Text)) {
                //MessageBox.Show("Need to upload all 3 profile");
                //return;
            }
            string connStr = GeneralVar.sqlServer;
            TimeSpan durationCard = TimeSpan.Zero;
            TimeSpan durationWallet = TimeSpan.Zero;
            TimeSpan durationDis = TimeSpan.Zero;
            TimeSpan duration = TimeSpan.Zero;
            int i = 0;
            if (i == 1)
            {
                if (!string.IsNullOrEmpty(cardFilePath.Text))
                {
                    DateTime startTimeCard = DateTime.Now;
                    await Upload(connStr, cardFilePath.Text, ProfileType.Card, cardOp);
                    DateTime endTimeCard = DateTime.Now;
                    durationCard = endTimeCard - startTimeCard;
                }

                if (!string.IsNullOrEmpty(walletFilePath.Text))
                {
                    DateTime startTimeWallet = DateTime.Now;
                    await Upload(connStr, walletFilePath.Text, ProfileType.Wallet, walletOp);//done
                    DateTime endTimeWallet = DateTime.Now;
                    durationWallet = endTimeWallet - startTimeWallet;

                }
                if (!string.IsNullOrEmpty(discountFilePath.Text))
                {
                    DateTime startTimeDis = DateTime.Now;
                    await Upload(connStr, discountFilePath.Text, ProfileType.Discount, discountOp);
                    DateTime endTimeDis = DateTime.Now;
                    durationDis = endTimeDis - startTimeDis;
                }
                MessageBox.Show("Time taken\n" +
                "Card: " + durationCard.TotalSeconds + " seconds\n" +
                "Wallet: " + durationWallet.TotalSeconds + " seconds\n" +
                "Discount: " + durationDis.TotalSeconds + " seconds\n"
                );
            }
            else {
                DateTime startTime = DateTime.Now;
                await UploadAll(connStr, discountFilePath.Text, cardFilePath.Text, walletFilePath.Text);
                DateTime endTime = DateTime.Now;
                duration = endTime - startTime;
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadFiles", null, null, string.Format("Total Time Taken: {0} Seconds", (int)duration.TotalSeconds));

                var durations = (endTime - startTime).TotalMinutes;
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, $"Total time take: {durations:F2} minutes");

                GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadFiles", null, null, string.Format("Complete"));
            }
            

        }
        private async Task UploadAll(string connectionString, string dis, string card, string wallet) {
            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Start"));
            
            string cardFilePath = Path.Combine(Path.GetTempPath(), "CardProfile_Cleaned.csv");
            HashSet<string> seenCard = new HashSet<string>();
            var cleanedLines = File.ReadLines(card)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim().TrimEnd(','))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    if (parts.Length == 3)
                    {
                        for (int i = 0; i < parts.Length; i++)
                        {
                            parts[i] = parts[i]
                                .Replace("\r", "")   // remove carriage return
                                .Replace("\n", "")   // remove line feed
                                .Trim('"')           // remove quotes
                                .Trim();             // remove spaces/tabs
                            parts[i] = parts[i];
                        }
                        string key = parts[0];

                        if (seenCard.Contains(key))
                        {
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Skip duplicate card:{0}", key));
                            return null;
                        }
                        seenCard.Add(key);
                        return string.Join(",", parts) + ",";

                    }
                    else if (parts.Length == 4)
                    {
                        for (int i = 0; i < parts.Length; i++)
                        {
                            parts[i] = parts[i]
                                .Replace("\r", "")   // remove carriage return
                                .Replace("\n", "")   // remove line feed
                                .Trim('"')           // remove quotes
                                .Trim();             // remove spaces/tabs
                            parts[i] = parts[i];
                        }
                        string key = parts[0];

                        if (seenCard.Contains(key))
                        {
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Skip duplicate card:{0}", key));
                            return null;
                        }
                        seenCard.Add(key);
                        return string.Join(",", parts);
                    }
                    else
                    {
                        return null;
                    }
                })
                .Where(line => line != null)
                .ToList();
            File.WriteAllLines(cardFilePath, cleanedLines);
            card = cardFilePath;



            string walletFilePath = Path.Combine(Path.GetTempPath(), "WalletProfile_Cleaned.csv");

            HashSet<string> seenWallet = new HashSet<string>();
            var cleanedLinesWallet = File.ReadLines(wallet)//wallet is ori file path
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim().TrimEnd(','))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    
                    if (parts.Length == 5)
                    {
                        for (int i = 0; i < parts.Length; i++)
                        {
                            parts[i] = parts[i]
                                .Replace("\r", "")   // remove carriage return
                                .Replace("\n", "")   // remove line feed
                                .Trim('"')           // remove quotes
                                .Trim();             // remove spaces/tabs
                            parts[i] = parts[i];
                        }
                        
                        string key = parts[4];

                        if (seenWallet.Contains(key))
                        {
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Skip duplicate wallet:{0}", key ));
                            return null;
                        }
                        seenWallet.Add(key);


                        return string.Join(",", parts);
                    }
                    return null;
                })
                .Where(line => line != null)
                .ToList();
            File.WriteAllLines(walletFilePath, cleanedLinesWallet);



            string discountFilePath = Path.Combine(Path.GetTempPath(), "DiscountProfile_Cleaned.csv");

            var cleanedLinesDiscount = File.ReadLines(dis)//wallet is ori file path
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim().TrimEnd(','))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    if (parts.Length == 4)
                    {
                        parts[0] = parts[0].Trim('"');
                        parts[1] = parts[1].Trim('"');
                        parts[2] = parts[2].Trim('"');
                        parts[3] = parts[3].Trim('"');


                        return string.Join(",", parts);
                    }
                    return null;
                })
                .Where(line => line != null)
                .ToList();



            File.WriteAllLines(discountFilePath, cleanedLinesDiscount);


            //string mysqlUploadsPath = @"C:\ProgramData\MySQL\MySQL Server 9.4\Uploads\";
            string mysqlUploadsPath = GeneralVar.mysqlUploadsPath;
            if (!Directory.Exists(mysqlUploadsPath))
            {
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Path not exist: {0}", mysqlUploadsPath));
                throw new DirectoryNotFoundException($"MySQL secure_file_priv folder not found: {mysqlUploadsPath}");
            }
            else {
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Path exist: {0}", mysqlUploadsPath));
            }
            string mysqlWalletFilePath = Path.Combine(mysqlUploadsPath, "WalletProfile_Cleaned.csv");
            string mysqlCardFilePath = Path.Combine(mysqlUploadsPath, "CardProfile_Cleaned.csv");
            string mysqlDiscountFilePath = Path.Combine(mysqlUploadsPath, "DiscountProfile_Cleaned.csv");

            // Copy the cleaned file to MySQL uploads folder
            File.Copy(walletFilePath, mysqlWalletFilePath, overwrite: true);
            File.Copy(cardFilePath, mysqlCardFilePath, overwrite: true);
            File.Copy(discountFilePath, mysqlDiscountFilePath, overwrite: true);

            // Update variable to point to MySQL uploads folder
            wallet = mysqlWalletFilePath;
            card = mysqlCardFilePath;
            dis = mysqlDiscountFilePath;
            
            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Wallet path: {0}",  wallet));
            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Discount path: {0}", dis));
            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Card path: {0}", card));
            

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    string fixedWalletPath = wallet.Replace("\\", "/");
                    string fixedCardPath = card.Replace("\\", "/");
                    string fixedDisPath = dis.Replace("\\", "/");
                    GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Wallet path special: {0}", fixedWalletPath));
                    
                    string sqlCard = $@"
                        SET GLOBAL local_infile = 1;
                        -- card part
                        DROP TABLE IF EXISTS CardProfile_temp;

                        

                        CREATE TABLE CardProfile_temp (
                            cardMfgNo CHAR(10) NOT NULL,
                            updatedTimestamp VARCHAR(40) NOT NULL,
                            walletUUID VARCHAR(100) NOT NULL,
                            discountPlan VARCHAR(5) NULL
                        );
                        
                        LOAD DATA INFILE '{fixedCardPath}'
                        INTO TABLE CardProfile_temp
                        FIELDS TERMINATED BY ','
                        OPTIONALLY ENCLOSED BY '""'
                        LINES TERMINATED BY '\n'
                        IGNORE 0 LINES
                        (cardMfgNo, updatedTimestamp, walletUUID, discountPlan);
                        
                        CREATE INDEX idx_cardMfgNo_time ON CardProfile_temp(cardMfgNo, updatedTimestamp);


                        TRUNCATE TABLE CardProfile;
                        INSERT INTO CardProfile (cardMfgNo, updatedTimestamp, walletUUID, discountPlan)
                        SELECT t.cardMfgNo, t.updatedTimestamp, t.walletUUID, t.discountPlan
                        FROM CardProfile_temp t
                        INNER JOIN (
                            SELECT cardMfgNo, MAX(updatedTimestamp) AS latestTime
                            FROM CardProfile_temp
                            GROUP BY cardMfgNo
                        ) m ON t.cardMfgNo = m.cardMfgNo AND t.updatedTimestamp = m.latestTime;



                        -- DROP TABLE CardProfile_temp;
                        SET GLOBAL local_infile = 0;
                        
                    ";
                    sqlCard = $@"
                        SET GLOBAL local_infile = 1;
                        -- card part
                        ALTER TABLE CardProfile DROP PRIMARY KEY;


                        TRUNCATE TABLE CardProfile;
                        LOAD DATA INFILE '{fixedCardPath}'
                        INTO TABLE CardProfile
                        FIELDS TERMINATED BY ','
                        OPTIONALLY ENCLOSED BY '""'
                        LINES TERMINATED BY '\r\n'
                        IGNORE 0 LINES
                        (cardMfgNo, updatedTimestamp, walletUUID, discountPlan);
                        ALTER TABLE CardProfile 
                        ADD PRIMARY KEY (cardMfgNo);


                        SET GLOBAL local_infile = 0;
                        
                    ";

                    string sqlWallet = $@"
                        SET GLOBAL local_infile = 1;
                        -- card part
                        DROP TABLE IF EXISTS WalletProfile_temp;

                        

                        CREATE TABLE WalletProfile_temp (
                            walletStatus CHAR(20) NOT NULL,
                            ledgerBalance FLOAT NOT NULL,
                            updatedTimestamp VARCHAR(40) NOT NULL,
                            directDebit CHAR(1) NOT NULL, 
                            walletUUID VARCHAR(100) NOT NULL
                        );
                        
                        LOAD DATA INFILE '{fixedWalletPath}'
                        INTO TABLE WalletProfile_temp
                        FIELDS TERMINATED BY ','
                        OPTIONALLY ENCLOSED BY '""'
                        LINES TERMINATED BY '\n'
                        IGNORE 0 LINES
                        (walletStatus, ledgerBalance, updatedTimestamp, directDebit, walletUUID);
                        
                        CREATE INDEX walletUUID ON WalletProfile_temp(walletUUID, updatedTimestamp);

                        TRUNCATE TABLE WalletProfile;

                        INSERT INTO WalletProfile (walletStatus, ledgerBalance, updatedTimestamp, directDebit, walletUUID)
                        SELECT t.walletStatus, t.ledgerBalance, t.updatedTimestamp, t.directDebit, t.walletUUID
                        FROM WalletProfile_temp t
                        INNER JOIN (
                            SELECT walletUUID, MAX(updatedTimestamp) AS latestTime
                            FROM WalletProfile_temp
                            GROUP BY walletUUID
                        ) m ON t.walletUUID = m.walletUUID AND t.updatedTimestamp = m.latestTime;



                        -- DROP TABLE WalletProfile_temp;
                        SET GLOBAL local_infile = 0;
                        
                    ";
                    sqlWallet = $@"
                        SET GLOBAL local_infile = 1;
                        -- card part
                        TRUNCATE TABLE WalletProfile;

                        LOAD DATA INFILE '{fixedWalletPath}'
                        INTO TABLE WalletProfile
                        FIELDS TERMINATED BY ','
                        OPTIONALLY ENCLOSED BY '""'
                        LINES TERMINATED BY '\r\n'
                        IGNORE 0 LINES
                        (walletStatus, ledgerBalance, updatedTimestamp, directDebit, walletUUID);

                        SET GLOBAL local_infile = 0;
                        
                    ";

                    string sqlDiscount = $@"
                        SET GLOBAL local_infile = 1;
                        -- card part
                        DROP TABLE IF EXISTS DiscountProfile_temp;

                        

                        CREATE TABLE DiscountProfile_temp (
                            discountPlan VARCHAR(10),
                            discountRate FLOAT,
                            updatedTimestamp VARCHAR(40),
                            effectiveDateTime VARCHAR(255)
                        );
                        
                        LOAD DATA INFILE '{fixedDisPath}'
                        INTO TABLE DiscountProfile_temp
                        FIELDS TERMINATED BY ','
                        OPTIONALLY ENCLOSED BY '""'
                        LINES TERMINATED BY '\n'
                        IGNORE 0 LINES
                        (discountPlan, discountRate, updatedTimestamp, effectiveDateTime);
                        
                        CREATE INDEX discountPlan ON DiscountProfile_temp(discountPlan, effectiveDateTime);
                        
                        TRUNCATE TABLE DiscountProfile;

                        INSERT INTO DiscountProfile (discountPlan, discountRate, updatedTimestamp, effectiveDateTime)
                        SELECT discountPlan, discountRate, updatedTimestamp, effectiveDateTime
                        FROM (
                            SELECT
                                discountPlan,
                                discountRate,
                                updatedTimestamp,
                                effectiveDateTime,
                                ROW_NUMBER() OVER (
                                    PARTITION BY
                                        discountPlan,
                                        effectiveDateTime
                                    ORDER BY STR_TO_DATE(LEFT(updatedTimestamp, 23), '%Y-%m-%dT%H:%i:%s.%f') DESC
                                ) AS rn
                            FROM DiscountProfile_temp
                        ) AS cleanData
                        WHERE rn = 1;



                        -- DROP TABLE DiscountProfile_temp;
                        SET GLOBAL local_infile = 0;
                        
                        
                    
                    ";
                    
                    //try {
                    //    var cardTask = RunSql(sqlCard, connectionString, "card");
                    //    var walletTask = RunSql(sqlWallet, connectionString, "wallet");
                    //    await Task.WhenAll(cardTask, walletTask);

                    //}
                    //catch (MySqlException ex)
                    //{
                    //    GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                    //    MessageBox.Show("Upload issue 1: " + ex.Message);
                    //}
                    //catch (Exception ex)
                    //{
                    //    GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                    //    MessageBox.Show("Upload issue 1: " + ex.Message);
                    //}


                    try
                    {
                        using (var cmd = new MySqlCommand(sqlDiscount, conn))
                        {
                            DateTime now = DateTime.Now;
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Start Execeute dis: " + now.ToString("yyyy-MM-dd HH:mm:ss")));

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                            DateTime settledis = DateTime.Now;
                            var duration = (settledis - now).TotalMinutes;
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Settle Execeute dis: " + settledis.ToString("yyyy-MM-dd HH:mm:ss")));
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, $"Discount execution completed in {duration:F2} minutes");
                        }


                        using (var cmd = new MySqlCommand(sqlCard, conn))
                        {
                            DateTime now = DateTime.Now;
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Start Execeute card: " + now.ToString("yyyy-MM-dd HH:mm:ss")));

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                            DateTime settle = DateTime.Now;
                            var duration = (settle - now).TotalMinutes;
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Settle Execeute card: " + settle.ToString("yyyy-MM-dd HH:mm:ss")));
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, $"Card execution completed in {duration:F2} minutes");
                        }




                        using (var cmd = new MySqlCommand(sqlWallet, conn))
                        {
                            DateTime now = DateTime.Now;
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Start Execeute wallet: " + now.ToString("yyyy-MM-dd HH:mm:ss")));

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                            DateTime settlewallet = DateTime.Now;
                            var duration = (settlewallet - now).TotalMinutes;
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Settle Execeute wallet: " + settlewallet.ToString("yyyy-MM-dd HH:mm:ss")));
                            GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, $"Wallet execution completed in {duration:F2} minutes");
                        }



                    }
                    catch (MySqlException ex)
                    {
                        GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                        MessageBox.Show("Upload issue: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                        MessageBox.Show("Upload issue 2: " + ex.Message);
                    }
                }
            }
            catch (MySqlException ex)
            {
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                MessageBox.Show("Upload issue 3: " + ex.Message);
            }
            catch (Exception ex)
            {
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                MessageBox.Show("Upload issue 4: " + ex.Message);
            }
            

        }

        private async Task RunSql(string sql, string connStr, string profile)
        {
            try {
                using (var conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        DateTime now = DateTime.Now;
                        GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Start Execeute for " + profile + " :" + now.ToString("yyyy-MM-dd HH:mm:ss")));
                        await cmd.ExecuteNonQueryAsync();
                    }
                    DateTime settle = DateTime.Now;
                    GeneralVar.Logger.WriteLog(Logger.Loglevel.Info, "UploadAll", null, null, string.Format("Settle Execeute for " + profile + " :" + settle.ToString("yyyy-MM-dd HH:mm:ss")));
                }
            }
            catch (MySqlException ex)
            {
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                MessageBox.Show("Upload issue 3: " + ex.Message);
            }
            catch (Exception ex)
            {
                GeneralVar.Logger.WriteLog(Logger.Loglevel.Error, "UploadAll", null, null, null, ex);
                MessageBox.Show("Upload issue 4: " + ex.Message);
            }
            
        }

        private async Task Upload(string connectionString, string filePath, ProfileType profileType, string operationType) { 

            
            if (profileType == ProfileType.Discount) {
                if (operationType == "Update" || operationType == "Insert") {
                    string tableName = "DiscountProfile";
                    string tempTable = "DiscountProfile_temp";
                    string sql = $@"
                        IF OBJECT_ID('{tempTable}', 'U') IS NOT NULL
                            DROP TABLE {tempTable};

                        CREATE TABLE {tempTable} (
                            discountPlan VARCHAR(10),
                            discountRate FLOAT,
                            updatedTimestamp VARCHAR(40),
                            effectiveDateTime VARCHAR(40)
                        );

                        BULK INSERT {tempTable}
                        FROM '{filePath}'
                        WITH (
                            FIELDTERMINATOR = ',',
                            ROWTERMINATOR = '\n',
                            FIRSTROW = 1,
                            CODEPAGE = '65001',
                            TABLOCK
                        );

                        MERGE {tableName} AS Target
                        USING (
                            SELECT discountPlan, discountRate, updatedTimestamp, effectiveDateTime
                            FROM (
                                SELECT
                                    LTRIM(RTRIM(REPLACE(discountPlan, '""', ''))) AS discountPlan,
                                    discountRate,
                                    LTRIM(RTRIM(REPLACE(updatedTimestamp, '""', ''))) AS updatedTimestamp,
                                    LTRIM(RTRIM(REPLACE(effectiveDateTime, '""', ''))) AS effectiveDateTime,
                                    ROW_NUMBER() OVER (
                                        PARTITION BY
                                            LTRIM(RTRIM(REPLACE(discountPlan, '""', ''))),
                                            LTRIM(RTRIM(REPLACE(effectiveDateTime, '""', '')))
                                        ORDER BY
                                            CAST(LTRIM(RTRIM(REPLACE(updatedTimestamp, '""', ''))) AS DATETIMEOFFSET) DESC
                                    ) AS rn
                                FROM {tempTable}
                            ) AS Deduped
                            WHERE rn = 1
                        ) AS Source

                        ON Target.discountPlan = Source.discountPlan AND Target.effectiveDateTime = Source.effectiveDateTime
                        WHEN MATCHED THEN
                            UPDATE SET
                                Target.discountRate = Source.discountRate,
                                Target.updatedTimestamp = Source.updatedTimestamp,
                                Target.isDeleted = 0
                        WHEN NOT MATCHED THEN
                            INSERT (discountPlan, discountRate, updatedTimestamp, effectiveDateTime)
                            VALUES (Source.discountPlan, Source.discountRate, Source.updatedTimestamp, Source.effectiveDateTime);

                        DROP TABLE {tempTable};

                        ";
                    sql = $@"
                        IF OBJECT_ID('{tempTable}', 'U') IS NOT NULL
                            DROP TABLE {tempTable};

                        CREATE TABLE {tempTable} (
                            discountPlan VARCHAR(10),
                            discountRate FLOAT,
                            updatedTimestamp VARCHAR(40),
                            effectiveDateTime VARCHAR(40)
                        );

                        BULK INSERT {tempTable}
                        FROM '{filePath}'
                        WITH (
                            FIELDTERMINATOR = ',',
                            ROWTERMINATOR = '\n',
                            FIRSTROW = 1,
                            CODEPAGE = '65001',
                            TABLOCK
                        );

                        
                        TRUNCATE TABLE {tableName};

                        
                        INSERT INTO {tableName} (discountPlan, discountRate, updatedTimestamp, effectiveDateTime)
                        SELECT discountPlan, discountRate, updatedTimestamp, effectiveDateTime
                        FROM (
                            SELECT
                                LTRIM(RTRIM(REPLACE(discountPlan, '""', ''))) AS discountPlan,
                                discountRate,
                                LTRIM(RTRIM(REPLACE(updatedTimestamp, '""', ''))) AS updatedTimestamp,
                                LTRIM(RTRIM(REPLACE(effectiveDateTime, '""', ''))) AS effectiveDateTime,
                                ROW_NUMBER() OVER (
                                    PARTITION BY
                                        LTRIM(RTRIM(REPLACE(discountPlan, '""', ''))),
                                        LTRIM(RTRIM(REPLACE(effectiveDateTime, '""', '')))
                                    ORDER BY
                                        CAST(LTRIM(RTRIM(REPLACE(updatedTimestamp, '""', ''))) AS DATETIMEOFFSET) DESC
                                ) AS rn
                            FROM {tempTable}
                        ) AS Deduped
                        WHERE rn = 1;

                        DROP TABLE {tempTable};
                        ";

                    await Task.Run(() =>
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();
                                using (SqlCommand cmd = new SqlCommand(sql, conn))
                                {
                                    cmd.CommandTimeout = 3000;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (SqlException ex)
                            {
                                Console.WriteLine("SQL Exception occurred:");
                                MessageBox.Show("Discount issue: " + ex.Message);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("General Exception occurred:");
                                MessageBox.Show("G Discount issue: " + ex.Message);
                            }
                        }
                    });
                }
            }
            else if (profileType == ProfileType.Card)
            {
                if (operationType == "Update" || operationType == "Insert") {
                    string tableName = "CardProfile";
                    string temp_table = "CardProfile_temp";

                    string tempFilePath = Path.Combine(Path.GetTempPath(), "CardProfile_Cleaned.csv");

                    var cleanedLines = File.ReadLines(filePath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(line => line.Trim().TrimEnd(','))
                        .Select(line =>
                        {
                            var parts = line.Split(',');
                            if (parts.Length == 3)
                            {
                                parts = parts.Select(p => p.Trim('"')).ToArray();
                                return string.Join(",", parts) + ",";
                            }
                            else if (parts.Length == 4)
                            {
                                parts = parts.Select(p => p.Trim('"')).ToArray();
                                return string.Join(",", parts);
                            }
                            else
                            {
                                return null;
                            }
                        })
                        .Where(line => line != null)
                        .ToList();


                    File.WriteAllLines(tempFilePath, cleanedLines);

                    
                    filePath = tempFilePath;


                    string sql = $@"
                        IF OBJECT_ID('{temp_table}', 'U') IS NOT NULL
                            DROP TABLE {temp_table};

                        CREATE TABLE {temp_table} (
                            cardMfgNo VARCHAR(40),
                            updatedTimestamp VARCHAR(50) NOT NULL,
                            walletUUID VARCHAR(100) NOT NULL,
                            discountPlan VARCHAR(100) NULL
                        );

                        BULK INSERT {temp_table}
                        FROM '{filePath}'
                        WITH (
                            FIELDTERMINATOR = ',',
                            ROWTERMINATOR = '\n',
                            FIELDQUOTE = '""',
                            FIRSTROW = 1,
                            CODEPAGE = '65001',
                            TABLOCK
                        );

                        MERGE {tableName} AS Target
                        USING (
                            SELECT
                                LTRIM(RTRIM(cardMfgNo)) AS cardMfgNo,
                                LTRIM(RTRIM(updatedTimestamp)) AS updatedTimestamp,
                                LTRIM(RTRIM(walletUUID)) AS walletUUID,
                                LTRIM(RTRIM(discountPlan)) AS discountPlan
                            FROM {temp_table}
                        ) AS Source
                        ON Target.cardMfgNo = Source.cardMfgNo
                        WHEN MATCHED THEN
                            UPDATE SET
                                Target.updatedTimestamp = Source.updatedTimestamp,
                                Target.walletUUID = Source.walletUUID,
                                Target.discountPlan = Source.discountPlan,
                                Target.isDeleted = 0
                        WHEN NOT MATCHED THEN
                            INSERT (cardMfgNo, updatedTimestamp, walletUUID, discountPlan)
                            VALUES (Source.cardMfgNo, Source.updatedTimestamp, Source.walletUUID, Source.discountPlan);
                        DROP TABLE {temp_table};

                        ";
                    sql = $@"
                        IF OBJECT_ID('{temp_table}', 'U') IS NOT NULL
                            DROP TABLE {temp_table};

                        CREATE TABLE {temp_table} (
                            cardMfgNo VARCHAR(40),
                            updatedTimestamp VARCHAR(50) NOT NULL,
                            walletUUID VARCHAR(100) NOT NULL,
                            discountPlan VARCHAR(100) NULL
                        );
                        BULK INSERT {temp_table}
                        FROM '{filePath}'
                        WITH (
                            FIELDTERMINATOR = ',',
                            ROWTERMINATOR = '\n',
                            FIELDQUOTE = '""',
                            FIRSTROW = 1,
                            CODEPAGE = '65001',
                            TABLOCK
                        );
                        TRUNCATE TABLE {tableName};
                        INSERT INTO {tableName} (cardMfgNo, updatedTimestamp, walletUUID, discountPlan)
                        SELECT cardMfgNo, updatedTimestamp, walletUUID, discountPlan
                        FROM (
                            SELECT 
                                LTRIM(RTRIM(cardMfgNo)) AS cardMfgNo,
                                LTRIM(RTRIM(updatedTimestamp)) AS updatedTimestamp,
                                LTRIM(RTRIM(walletUUID)) AS walletUUID,
                                LTRIM(RTRIM(discountPlan)) AS discountPlan,
                                ROW_NUMBER() OVER (PARTITION BY cardMfgNo ORDER BY updatedTimestamp DESC) AS rn
                            FROM {temp_table}
                        ) AS cleanData
                        WHERE rn = 1;
                        DROP TABLE {temp_table};
                        ";


                    await Task.Run(() =>
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();
                                using (SqlCommand cmd = new SqlCommand(sql, conn))
                                {
                                    cmd.CommandTimeout = 3000;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (SqlException ex)
                            {
                                Console.WriteLine("SQL Exception occurred:");
                                MessageBox.Show("Card issue: " + ex.Message);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("General Exception occurred:");
                                MessageBox.Show("G Card issue: " + ex.Message);
                            }
                        }
                    });
                }
            }
            else if (profileType == ProfileType.Wallet)
            {
                if (operationType == "Update" || operationType == "Insert")
                {
                    string tableName = "WalletProfile";
                    string tempTable = "WalletProfile_temp";

                    string tempFilePath = Path.Combine(Path.GetTempPath(), "WalletProfile_Cleaned.csv");

                    var cleanedLines = File.ReadLines(filePath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(line => line.Trim().TrimEnd(','))
                        .Select(line =>
                        {
                            var parts = line.Split(',');
                            if (parts.Length == 5)
                            {
                                parts[0] = parts[0].Trim('"');
                                parts[1] = parts[1].Trim('"');
                                parts[2] = parts[2].Trim('"');
                                parts[3] = parts[3].Trim('"');
                                parts[4] = parts[4].Trim('"');


                                return string.Join(",", parts);
                            }
                            return null;
                        })
                        .Where(line => line != null)
                        .ToList();



                    File.WriteAllLines(tempFilePath, cleanedLines);


                    filePath = tempFilePath;


                    string sql = $@"
                    IF OBJECT_ID('{tempTable}', 'U') IS NOT NULL
                        DROP TABLE {tempTable};

                    CREATE TABLE {tempTable} (
                        walletStatus CHAR(20) NOT NULL,
                        ledgerBalance FLOAT NOT NULL,
                        updatedTimestamp VARCHAR(40) NOT NULL,
                        directDebit CHAR(1) NOT NULL, 
                        walletUUID VARCHAR(100) NOT NULL
                    );

                    BULK INSERT {tempTable}
                    FROM '{filePath}'
                    WITH (
                        FIELDTERMINATOR = ',',
                        ROWTERMINATOR = '\n',
                        FIELDQUOTE = '""', 
                        FIRSTROW = 1,
                        CODEPAGE = '65001',
                        TABLOCK
                    );
                    MERGE {tableName} AS Target
                    USING (
                        SELECT *
                        FROM (
                            SELECT 
                                LTRIM(RTRIM(walletStatus)) AS walletStatus,
                                ledgerBalance,
                                LTRIM(RTRIM(updatedTimestamp)) AS updatedTimestamp,
                                LTRIM(RTRIM(directDebit)) AS directDebit,
                                LTRIM(RTRIM(walletUUID)) AS walletUUID,
                                ROW_NUMBER() OVER (PARTITION BY walletUUID ORDER BY updatedTimestamp DESC) AS rn
                            FROM {tempTable}
                        ) AS temp
                        WHERE rn = 1
                    ) AS Source
                    ON Target.walletUUID = Source.walletUUID
                    WHEN MATCHED THEN
                        UPDATE SET
                            Target.updatedTimestamp = Source.updatedTimestamp,
                            Target.walletUUID = Source.walletUUID,
                            Target.ledgerBalance = Source.ledgerBalance,
                            Target.walletStatus = Source.walletStatus,
                            Target.directDebit = Source.directDebit,
                            Target.isDeleted = 0
                    WHEN NOT MATCHED THEN
                        INSERT (walletStatus, ledgerBalance, updatedTimestamp, directDebit, walletUUID)
                        VALUES (Source.walletStatus, Source.ledgerBalance, Source.updatedTimestamp, Source.directDebit, Source.walletUUID);

                    DROP TABLE {tempTable};

                    ";
                    sql = $@"
                        IF OBJECT_ID('{tempTable}', 'U') IS NOT NULL
                            DROP TABLE {tempTable};

                        CREATE TABLE {tempTable} (
                            walletStatus CHAR(20) NOT NULL,
                            ledgerBalance FLOAT NOT NULL,
                            updatedTimestamp VARCHAR(40) NOT NULL,
                            directDebit CHAR(1) NOT NULL, 
                            walletUUID VARCHAR(100) NOT NULL
                        );

                        BULK INSERT {tempTable}
                        FROM '{filePath}'
                        WITH (
                            FIELDTERMINATOR = ',',
                            ROWTERMINATOR = '\n',
                            FIELDQUOTE = '""', 
                            FIRSTROW = 1,
                            CODEPAGE = '65001',
                            TABLOCK
                        );

                        TRUNCATE TABLE {tableName};
                        INSERT INTO {tableName} (walletStatus, ledgerBalance, updatedTimestamp, directDebit, walletUUID)
                        SELECT walletStatus, ledgerBalance, updatedTimestamp, directDebit, walletUUID
                            FROM (
                                SELECT 
                                    LTRIM(RTRIM(walletStatus)) AS walletStatus,
                                    ledgerBalance,
                                    LTRIM(RTRIM(updatedTimestamp)) AS updatedTimestamp,
                                    LTRIM(RTRIM(directDebit)) AS directDebit,
                                    LTRIM(RTRIM(walletUUID)) AS walletUUID,
                                    ROW_NUMBER() OVER (PARTITION BY walletUUID ORDER BY updatedTimestamp DESC) AS rn
                                FROM {tempTable}
                            ) AS cleanData
                        WHERE rn = 1;
                        

                        DROP TABLE {tempTable};
                        ";


                    
                    await Task.Run(() =>
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            try
                            {
                                conn.Open();
                                using (SqlCommand cmd = new SqlCommand(sql, conn))
                                {
                                    cmd.CommandTimeout = 3000;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (SqlException ex)
                            {
                                Console.WriteLine("SQL Exception occurred:");
                                MessageBox.Show("Wallet issue: " + ex.Message);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("General Exception occurred:");
                                MessageBox.Show("G Wallet issue: " + ex.Message);
                            }
                        }
                    });

                }
            }
            
        }
        private void BrowseWalletFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                walletFilePath.Text = openFileDialog.FileName;
                //logBox.AppendText($"[Wallet] Selected file: {openFileDialog.FileName}\n");
            }
        }

        private void BrowseCardFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                cardFilePath.Text = openFileDialog.FileName;
                //logBox.AppendText($"[Card] Selected file: {openFileDialog.FileName}\n");
            }
        }

        private void BrowseDiscountFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                discountFilePath.Text = openFileDialog.FileName;
                //logBox.AppendText($"[Discount] Selected file: {openFileDialog.FileName}\n");
            }
        }

        private void TestDbConnection_Click(object sender, RoutedEventArgs e)
        {
            TestDatabaseConnection();
        }

        private void TestDatabaseConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    MessageBox.Show("Connection Successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Connection Failed:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public enum ProfileType
        {
            Card,
            Wallet,
            Discount
        }
    }

}
