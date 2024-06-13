using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System.IO;

public class DisplayModels : MonoBehaviour
{
    public string DataBaseName; // Database name
    public GameObject[] emptyGameObjects; // Array for empty GameObjects with fixed positions

    public static class SetDataBaseClass
    {
        public static string SetDataBase(string dbName)
        {
            string dbPath = "";
#if UNITY_EDITOR
            dbPath = Path.Combine(Application.streamingAssetsPath, dbName);
#elif UNITY_WSA
            string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, dbName);
            dbPath = Path.Combine(Application.persistentDataPath, dbName);

            if (!File.Exists(dbPath))
            {
                Debug.Log($"Copying database from {streamingAssetsPath} to {dbPath}");
                CopyDatabase(streamingAssetsPath, dbPath);
            }
#else
            dbPath = Path.Combine(Application.streamingAssetsPath, dbName);
#endif
            string connString = "URI=file:" + dbPath;
            Debug.Log("Database path: " + dbPath);
            return connString;
        }

        private static void CopyDatabase(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath, true);
        }
    }

    public void DisplayModelById(int productId)
    {
        Debug.Log("DisplayModelById method called with ID: " + productId);

        List<string> modelPaths = new List<string>();

        string conn = SetDataBaseClass.SetDataBase(DataBaseName + ".db");
        Debug.Log("Connection string: " + conn);

        using (IDbConnection dbcon = new SqliteConnection(conn))
        {
            dbcon.Open();
            Debug.Log("Database connection opened.");

            // Check if the table exists
            using (IDbCommand dbcmd = dbcon.CreateCommand())
            {
                dbcmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='produkte';";
                using (IDataReader reader = dbcmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        Debug.LogError("Table 'produkte' does not exist in the database.");
                        return;
                    }
                }
            }

            // Query the database for the product ID
            using (IDbCommand dbcmd = dbcon.CreateCommand())
            {
                string sqlQuery = "SELECT dateipfad FROM produkte WHERE id = @ID";
                dbcmd.CommandText = sqlQuery;

                var IDParam = dbcmd.CreateParameter();
                IDParam.ParameterName = "@ID";
                IDParam.Value = productId;
                dbcmd.Parameters.Add(IDParam);

                using (IDataReader reader = dbcmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        modelPaths.Add(reader.GetString(0));
                    }
                    else
                    {
                        Debug.LogError("No entry found for ID: " + productId);
                    }
                }
            }
        }

        // Check if there are enough empty GameObjects
        if (emptyGameObjects.Length < modelPaths.Count)
        {
            Debug.LogError("Not enough empty game objects available.");
            return;
        }

        // Remove any existing models from the empty GameObjects
        foreach (GameObject emptyGameObject in emptyGameObjects)
        {
            foreach (Transform child in emptyGameObject.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Assign models to empty GameObjects
        for (int i = 0; i < modelPaths.Count; i++)
        {
            string modelName = modelPaths[i]; // Filename without the extension
            GameObject modelPrefab = Resources.Load<GameObject>(modelName);

            if (modelPrefab != null && i < emptyGameObjects.Length)
            {
                GameObject emptyGameObject = emptyGameObjects[i];
                GameObject modelInstance = Instantiate(modelPrefab, emptyGameObject.transform);
                modelInstance.transform.localScale = new Vector3(500f, 500f, 500f); // Set scale as needed

                // Optional: Make other transformations if necessary
            }
            else
            {
                Debug.LogError("Unable to load model or not enough empty game objects available for model: " + modelName);
            }
        }
    }
}
