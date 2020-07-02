using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SixModLoader.Compatibility.Exiled
{
    public class WorkflowRunsResponse
    {
        [JsonProperty("total_count")]
        public long TotalCount { get; set; }

        [JsonProperty("workflow_runs")]
        public List<WorkflowRun> WorkflowRuns { get; set; }
    }

    public class WorkflowRun
    {
        [JsonProperty("head_branch")]
        public string HeadBranch { get; set; }
        
        [JsonProperty("artifacts_url")]
        public Uri ArtifactsUrl { get; set; }
    }
    
    public class WorkflowArtifactsResponse
    {
        [JsonProperty("total_count")]
        public long TotalCount { get; set; }

        [JsonProperty("artifacts")]
        public List<WorkflowArtifact> WorkflowArtifacts { get; set; }
    }
    
    public class WorkflowArtifact
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("archive_download_url")]
        public Uri ArchiveDownloadUrl { get; set; }
    }
}