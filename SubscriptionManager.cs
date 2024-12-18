﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

public static class SubscriptionManager
{
    private const string SubscriptionsFileName = "subscriptions.txt";
    public static List<string> SubscribedAuthors { get; private set; }

    static SubscriptionManager()
    {
        SubscribedAuthors = new List<string>();
        LoadSubscriptions(); 
    }

    public static void Subscribe(string channelId)
    {
        if (!SubscribedAuthors.Contains(channelId))
        {
            SubscribedAuthors.Add(channelId);
            SaveSubscriptions();
        }
    }

    public static void Unsubscribe(string channelId)
    {
        if (SubscribedAuthors.Contains(channelId))
        {
            SubscribedAuthors.Remove(channelId);
            SaveSubscriptions();
        }
    }

    public static bool IsSubscribed(string channelId)
    {
        return SubscribedAuthors.Contains(channelId);
    }

    public static async Task ImportSubscriptionsFromCsv(StorageFile file)
    {
        try
        {
            using (var stream = await file.OpenStreamForReadAsync())
            using (var reader = new StreamReader(stream))
            {
                string line;
                bool isFirstLine = true;

                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    var columns = line.Split(',');

                    if (columns.Length >= 1)
                    {
                        string channelId = columns[0].Trim();
                        Subscribe(channelId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error importing subscriptions: " + ex.Message);
        }
    }

    public static void SaveSubscriptions()
    {
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = folder.CreateFileAsync(SubscriptionsFileName, CreationCollisionOption.ReplaceExisting).AsTask().Result;

            var serializedIds = string.Join(",", SubscribedAuthors);
            FileIO.WriteTextAsync(file, serializedIds).AsTask().Wait();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error saving subscriptions: " + ex.Message);
        }
    }

    public static void LoadSubscriptions()
    {
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = folder.GetFileAsync(SubscriptionsFileName).AsTask().Result;

            var serializedIds = FileIO.ReadTextAsync(file).AsTask().Result;
            if (!string.IsNullOrEmpty(serializedIds))
            {
                SubscribedAuthors = serializedIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                System.Diagnostics.Debug.WriteLine("Subscriptions successfully loaded.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No subscriptions found in the file.");
                SubscribedAuthors = new List<string>();
            }
        }
        catch (FileNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine("No subscriptions file found.");
            SubscribedAuthors = new List<string>(); 
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error loading subscriptions: " + ex.Message);
            SubscribedAuthors = new List<string>();
        }
    }

    public static void ClearSubscriptions()
    {
        try
        {
            SubscribedAuthors.Clear();
            SaveSubscriptions();
            System.Diagnostics.Debug.WriteLine("All subscriptions have been cleared.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error clearing subscriptions: " + ex.Message);
        }
    }
}