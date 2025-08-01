using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Core.Utilities.AsyncAwaitUtil.Source;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Configurations.GoogleSheets
{
    public static class TableUtilities
    {
        public static async Task<string> LoadAsync(string url)
        {
            var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <param name="csvTable"></param>
        /// <param name="separator"></param>
        /// <param name="dataOutput">Format: (string column, int row, string value)</param>
        public static void ProcessTable(string csvTable, char separator, Action<string, int, string> dataOutput)
        {
            var rows = csvTable.Split('\n');
            var names = rows[0].Split(separator);
            for (int i = 1; i < rows.Length; i++)
            {
                var cells = rows[i].Split(separator);
                for (var j = 0; j < cells.Length; j++)
                {
                    dataOutput(names[j], i, cells[i]);
                }
            }
        }

        /*public static T[] ParseAs<T>(string csvTable, char separator) where T : new()
        {
            Dictionary<string, FieldInfo> fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToDictionary(k => k.Name.ToLower());
            var rows = csvTable.Split(new []{'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            var names = rows[0].ToLower().Split(separator);
            T[] result = new T[rows.Length - 1];
            for (int row = 1; row < rows.Length; row++)
            {
                var cells = rows[row].Split(separator);
                result[row - 1] = new T();

                for (var column = 0; column < cells.Length; column++)
                {
                    if (fields.TryGetValue(names[column], out var field))
                    {
                        if (field.FieldType == typeof(string))
                        {
                            field.SetValue(result[row - 1], cells[column]);
                        }
                        else if (field.FieldType == typeof(int))
                        {
                            if (int.TryParse(cells[column], out int value))
                            {
                                field.SetValue(result[row - 1], value);
                            }
                        }else if (field.FieldType == typeof(int))
                        {
                            if (float.TryParse(cells[column], out float value))
                            {
                                field.SetValue(result[row - 1], value);
                            }
                        }
                    }
                }
            }

            return result;
        }*/

        public static T[] ParseAs<T>(string csvTable, char separator, char arraySeparator) where T : new()
        {
            Dictionary<string, FieldInfo> fields = typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .ToDictionary(k => k.Name.ToLower(), v => v);

            string[] rows = csvTable.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            string[] names = rows[0].Trim().ToLower().Split(separator);

            T[] result = new T[rows.Length - 1];

            for (int rowIndex = 1; rowIndex < rows.Length; rowIndex++)
            {
                string[] cells = rows[rowIndex].Trim().Split(separator);

                result[rowIndex - 1] = new T();

                for (int colIndex = 0; colIndex < cells.Length && colIndex < names.Length; colIndex++)
                {
                    string name = names[colIndex];

                    string cellValue = cells[colIndex].Trim();

                    if (!fields.TryGetValue(name, out FieldInfo field)) continue;

                    Type fieldType = field.FieldType;

                    if (fieldType.IsPrimitive || fieldType == typeof(string))
                    {
                        try
                        {
                            field.SetValue(result[rowIndex - 1], GetValue(field.FieldType, cellValue));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    else if (fieldType.IsArray && fieldType.GetElementType() != null &&
                             (fieldType.GetElementType().IsPrimitive || fieldType.GetElementType() == typeof(string)))
                    {
                        var values = cellValue.Split(arraySeparator);
                        Array arrayValues = Array.CreateInstance(fieldType.GetElementType(), values.Length);

                        for (int i = 0; i < values.Length; i++)
                        {
                            arrayValues.SetValue(GetValue(fieldType.GetElementType(), values[i].Trim()), i);
                        }

                        field.SetValue(result[rowIndex - 1], arrayValues);
                    }
                }
            }

            return result;
        }

        private static object GetValue(Type fieldType, string value)
        {
            switch (fieldType.Name)
            {
                case nameof(Int32):
                    if (Int32.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int iVal))
                    {
                        return iVal;
                    }

                    break;
                case nameof(Single):
                    if (Single.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float fVal))
                    {
                        return fVal;
                    }

                    break;
                case nameof(Double):
                    if (Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal))
                    {
                        return dVal;
                    }

                    break;
                default:
                    return value; // Строки и прочие типы задаются напрямую
            }

            return null;
        }
    }
}