using System;
using System.Collections.Generic;
using UnityEngine;

public class LocationJSONLoader: MonoBehaviour
{
    private const string ResourceFileName = "test";
    private RoadProjectDatabase roadProjectDatabase;

    private void Start()
    {
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(ResourceFileName);
        if (jsonFile == null)
        {
            Debug.LogError($"[LocationJSONLoader] Could not find {ResourceFileName}.json in Resources.");
            return;
        }

        roadProjectDatabase = JsonUtility.FromJson<RoadProjectDatabase>(jsonFile.text);

        int roadCount = roadProjectDatabase?.roads?.Length ?? 0;
        int projectCount = roadProjectDatabase?.projects?.Length ?? 0;
        
        if (roadCount == 0 || projectCount == 0)
        {
            Debug.LogError($"[LocationJSONLoader] JSON Loaded, but roads/projects are missing or empty");
            return;
        }

        Debug.Log($"[LocationJSONLoader] Loaded roads={roadCount}, projects={projectCount}");    
    }

    public ProjectRecord[] GetProjectsForRoad (string roadName)
    {
        if(!TryFindMatchingRoadEntry (roadName, out RoadEntry matchedRoad))
        {
            return null;
        }

        if(matchedRoad.projectIds == null || matchedRoad.projectIds.Length == 0)
        {
            return Array.Empty<ProjectRecord>();
        }

        if (roadProjectDatabase == null || roadProjectDatabase.projects == null)
        {
            return Array.Empty<ProjectRecord>();
        }
        
        List<ProjectRecord> resolvedProjects = new List<ProjectRecord>();

        foreach (string projectId in matchedRoad.projectIds)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                continue;
            }

            ProjectRecord project = FindProjectById(projectId);
            if (project != null)
            {
                resolvedProjects.Add(project);
            }
            else
            {
                Debug.LogWarning($"[LocationJSONLoader] Missing project for id:{projectId}");
            }   
        }
        return resolvedProjects.ToArray();
    }

    public int GetProjectCountForRoad(string roadName)
    {
        ProjectRecord[] projects = GetProjectsForRoad(roadName);
        return projects?.Length ?? 0;
    }

    public bool HasProjectsForRoad(string roadName)
    {
        return GetProjectCountForRoad(roadName) > 0;
    }

    public string GetBestMatchingRoadName (string roadName)
    {
        if (TryFindMatchingRoadEntry(roadName, out RoadEntry matchedRoad))
        {
            return matchedRoad.roadName;
        }
        return null;
    }

    private ProjectRecord FindProjectById(string projectId)
    {
        if (roadProjectDatabase == null || roadProjectDatabase.projects == null)
        {
            return null;
        }

        foreach (ProjectRecord project in roadProjectDatabase.projects)
        {
            if (project == null || string.IsNullOrEmpty(project.projectId))
            {
                continue;
            }

            if (string.Equals(project.projectId, projectId, StringComparison.OrdinalIgnoreCase))
            {
                return project;
            }
        }
        return null;
    }

    private bool TryFindMatchingRoadEntry (string roadName, out RoadEntry matchedRoad)
    {
        matchedRoad = null;

        if (string.IsNullOrEmpty (roadName) ||
            roadProjectDatabase == null ||
            roadProjectDatabase.roads == null)
        {
            return false;
        }

        foreach (RoadEntry road in roadProjectDatabase.roads)
        {
            if (road == null || string.IsNullOrEmpty(road.roadName))
            {
                continue;
            }

            if (road.roadName.IndexOf(roadName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                roadName.IndexOf(road.roadName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matchedRoad = road;
                return true;
            }
        }
        return false;
    }

}