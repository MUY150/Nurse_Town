using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ToolRegistry : Singleton<ToolRegistry>
{
    private Dictionary<string, ITool> _tools = new Dictionary<string, ITool>();
    private ToolContext _currentContext;
    
    public event Action<ToolCallEventArgs> OnToolCalled;
    public event Action<ToolExecutedEvent> OnToolExecuted;
    
    public ToolContext CurrentContext
    {
        get => _currentContext;
        set
        {
            _currentContext = value;
            UpdateDynamicTools();
        }
    }

    private void UpdateDynamicTools()
    {
        if (_currentContext == null) return;
        
        foreach (var tool in _tools.Values)
        {
            if (tool is IDynamicTool dynamicTool)
            {
                dynamicTool.SetContext(_currentContext);
            }
        }
    }
    
    public void RegisterTool(ITool tool)
    {
        if (tool == null || string.IsNullOrEmpty(tool.Name))
        {
            Debug.LogWarning("[ToolRegistry] Cannot register tool with null or empty name");
            return;
        }
        
        string key = tool.Name.ToLower();
        if (_tools.ContainsKey(key))
        {
            Debug.LogWarning($"[ToolRegistry] Tool '{tool.Name}' already registered, replacing");
        }
        
        _tools[key] = tool;
        
        if (tool is IDynamicTool dynamicTool && _currentContext != null)
        {
            dynamicTool.SetContext(_currentContext);
        }
        
        Debug.Log($"[ToolRegistry] Registered tool: {tool.Name}");
    }
    
    public void UnregisterTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return;
        
        string key = toolName.ToLower();
        if (_tools.Remove(key))
        {
            Debug.Log($"[ToolRegistry] Unregistered tool: {toolName}");
        }
    }
    
    public ITool GetTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return null;
        
        string key = toolName.ToLower();
        return _tools.TryGetValue(key, out ITool tool) ? tool : null;
    }
    
    public List<ITool> GetAllTools()
    {
        return new List<ITool>(_tools.Values);
    }
    
    public ToolResult ExecuteTool(string toolName, JObject parameters)
    {
        var tool = GetTool(toolName);
        if (tool == null)
        {
            Debug.LogError($"[ToolRegistry] Tool '{toolName}' not found");
            return ToolResult.ErrorResult($"Tool '{toolName}' not found");
        }
        
        try
        {
            var result = tool.Execute(parameters);
            
            var args = new ToolCallEventArgs
            {
                ToolName = toolName,
                Parameters = parameters,
                Result = result
            };
            OnToolCalled?.Invoke(args);
            
            var toolEvent = new ToolExecutedEvent
            {
                Timestamp = DateTime.Now,
                ToolName = toolName,
                Parameters = parameters,
                Result = result
            };
            OnToolExecuted?.Invoke(toolEvent);
            LlmEventBus.Publish(toolEvent);
            
            Debug.Log($"[ToolRegistry] Executed tool '{toolName}': Success={result.Success}");
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ToolRegistry] Error executing tool '{toolName}': {e.Message}");
            return ToolResult.ErrorResult(e.Message);
        }
    }
    
    public JArray GetToolsSchema()
    {
        var schema = new JArray();
        foreach (var tool in _tools.Values)
        {
            JObject toolSchema;
            if (tool is IDynamicTool dynamicTool)
            {
                toolSchema = new JObject
                {
                    ["type"] = "function",
                    ["function"] = new JObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        ["parameters"] = dynamicTool.GetDynamicSchema()
                    }
                };
            }
            else
            {
                toolSchema = new JObject
                {
                    ["type"] = "function",
                    ["function"] = new JObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        ["parameters"] = tool.ParametersSchema
                    }
                };
            }
            schema.Add(toolSchema);
        }
        return schema;
    }
}
