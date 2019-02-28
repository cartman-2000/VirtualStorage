using MySql.Data.MySqlClient;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace VirtualStorage
{
    public static class Extensions
    {
        public static bool IsDBNull (this MySqlDataReader reader, string fieldname)
        {
            return reader.IsDBNull(reader.GetOrdinal(fieldname));
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
