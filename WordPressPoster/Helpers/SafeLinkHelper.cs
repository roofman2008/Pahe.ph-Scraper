using System;
using System.Linq;
using MySql.Data.MySqlClient;

namespace WordPressPoster.Helpers
{
    public static class SafeLinkHelper
    {
        static Random random = new Random();
        private const string cs = 
            @"server=localhost;
            userid=root;
            password=123456789;
            database=wordpress";

        private static string GetRandomHexNumber(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        private static string GetRandomSafeId()
        {
            /*Need To Check with MySql if the SafeId already exist to regenerate new unique one*/
            return GetRandomHexNumber(8).ToLower();
        }

        public static string AddLink(string link)
        {
            string safeId = GetRandomSafeId();

            MySqlConnection conn = null;

            try
            {
                conn = new MySqlConnection(cs);
                conn.Open();

                string stm = $@"INSERT INTO `wp_wpsafelink`
                                (`ID`, `date`, `date_view`, `date_click`, `safe_id`, `link`, `view`, `click`) VALUES
                                (0,CURDATE(),CURDATE(),CURDATE(),'{safeId}','{link}',0,0)";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                conn?.Close();
                return null;
            }
            finally
            {
                conn?.Close();
            }

            return safeId;
        }

        public static void RemoveLink(string safeId)
        {
            MySqlConnection conn = null;

            try
            {
                conn = new MySqlConnection(cs);
                conn.Open();

                string stm = $@"DELETE FROM `wp_wpsafelink` WHERE `safe_id` = '{safeId}'";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                conn?.Close();
                throw ex;
            }
            finally
            {
                conn?.Close();
            }
        }
    }
}