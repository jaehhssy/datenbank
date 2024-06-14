using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System.IO;
#if UNITY_WSA && !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Storage;
using System;
#endif

public class DisplayModels : MonoBehaviour
{
    public string DataBaseName; // Database name
    public GameObject emptyGameObject = null;

    private void Start()
    {
    }

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
                    Debug.Log($"Database file does not exist at {dbPath}. Copying from {streamingAssetsPath}.");
                    CopyDatabase(streamingAssetsPath, dbPath).Wait();
                }
                else
                {
                    Debug.Log("Database already exists at: " + dbPath);
                }
#else
            dbPath = Path.Combine(Application.streamingAssetsPath, dbName);
#endif

            string connString = "URI=file:" + dbPath;
            Debug.Log("Database path: " + dbPath);
            return connString;
        }

#if UNITY_WSA && !UNITY_EDITOR
            private static async Task CopyDatabase(string sourcePath, string destinationPath)
            {
                try
                {
                    StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                    StorageFile destinationFile = await storageFolder.CreateFileAsync(Path.GetFileName(destinationPath), CreationCollisionOption.ReplaceExisting);

                    StorageFile sourceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(sourcePath));
                    await sourceFile.CopyAndReplaceAsync(destinationFile);

                    Debug.Log("Database copied successfully.");
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error copying database: " + ex.Message);
                }
            }
#else
        private static void CopyDatabase(string sourcePath, string destinationPath)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destinationPath, true);
                    Debug.Log("Database copied successfully.");
                }
                else
                {
                    Debug.LogError("Source database file does not exist: " + sourcePath);
                }
            }
            catch (IOException ex)
            {
                Debug.LogError("Error copying database: " + ex.Message);
            }
        }
#endif
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

        if (transform.childCount > 0)
        {
            GameObject childObject = transform.GetChild(0).gameObject;
            Destroy(childObject);
        }

        // Assign models to empty GameObjects
        for (int i = 0; i < modelPaths.Count; i++)
        {
            string modelName = modelPaths[i]; // Filename without the extension
            GameObject modelPrefab = Resources.Load<GameObject>(modelName);

            if (modelPrefab != null)
            {
                GameObject modelInstance = Instantiate(modelPrefab, gameObject.transform);
                modelInstance.transform.localScale = new Vector3(1f, 1f, 1f); // Set scale as needed

                // Optional: Make other transformations if necessary
            }
            else
            {
                Debug.LogError("Unable to load model or not enough empty game objects available for model: " + modelName);
            }
        }
    }

public void DisplayModelById1()
    {
        DisplayModelById(1);
    }
    public void DisplayModelById2()
    {
        DisplayModelById(2);
    }
}
