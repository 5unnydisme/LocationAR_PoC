using System;

[Serializable]
public class RoadProjectDatabase
{
    public RoadEntry[] roads;
    public ProjectRecord[] projects;
}

[Serializable]
public class RoadEntry
{
    public string roadName;
    public string[] projectIds;
}

[Serializable]
public class ProjectRecord
{
    public string projectId;
    public string workType;
    public string contractor;
    public string budget;
    public string startDate;
    public string endDate;
    public string length;
    public string stretchDescription;
}