# RAG Knowledge Retrieval System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为 Nurse Town 项目实现基于 ReAct 的知识检索系统，让 Agent 能够根据对话上下文智能读取医疗知识库文档，提升患者角色扮演的专业性和评价准确性。

**Architecture:** 通过 read_knowledge 工具让 LLM 按需读取知识文档，文档内容加入对话历史，LLM 基于知识生成回复。预留向量数据库接口以便后续扩展。

**Tech Stack:** C#, Unity, YAML 解析, ReAct 模式, 事件驱动架构

---

## Task 1: 创建知识文档目录结构和示例文档

**Files:**
- Create: `Assets/StreamingAssets/Knowledge/Hypertension/symptoms.md`
- Create: `Assets/StreamingAssets/Knowledge/Hypertension/diagnosis.md`
- Create: `Assets/StreamingAssets/Knowledge/Hypertension/nursing_care.md`
- Create: `Assets/StreamingAssets/Knowledge/Diabetes/symptoms.md`
- Create: `Assets/StreamingAssets/Knowledge/BrocaAphasia/symptoms.md`

**Step 1: 创建高血压症状文档**

```markdown
---
metadata:
  disease: "高血压"
  category: "症状"
  description: "高血压患者的常见症状，包括头痛、头晕、心悸等，以及不同严重程度的表现"
  keywords: ["头痛", "头晕", "心悸", "高血压症状", "血压升高"]
---

# 高血压患者常见症状

## 常见症状
高血压患者在血压升高时可能会出现以下症状：
- 头痛：通常位于后脑勺或太阳穴部位，持续性或阵发性
- 头晕：站立或突然改变体位时明显，可能伴随视物模糊
- 心悸：感觉心跳加速或不规则
- 耳鸣：持续性或间歇性耳鸣声

## 严重症状
当血压严重升高时（收缩压 > 180mmHg 或舒张压 > 120mmHg），可能出现：
- 剧烈胸痛：可能提示心脏问题
- 呼吸困难：可能提示心力衰竭
- 意识模糊或昏迷：可能提示高血压脑病
- 视力突然下降：可能提示视网膜病变

## 患者表现
作为高血压患者模拟时：
- 轻度升高：可能无明显症状，或仅有轻微头晕
- 中度升高：头痛、头晕明显，可能影响日常活动
- 重度升高：症状严重，需要紧急处理
```

**Step 2: 创建高血压诊断标准文档**

```markdown
---
metadata:
  disease: "高血压"
  category: "诊断"
  description: "高血压的诊断标准和分类，用于评估护士的诊断准确性"
  keywords: ["高血压诊断", "血压标准", "血压分类", "诊断标准"]
---

# 高血压诊断标准

## 血压测量标准
在安静状态下，非同日三次测量血压：

- 正常血压：收缩压 < 120mmHg 且 舒张压 < 80mmHg
- 正常高值：收缩压 120-139mmHg 或 舒张压 80-89mmHg
- 高血压：收缩压 ≥ 140mmHg 或 舒张压 ≥ 90mmHg

## 高血压分类
1级高血压（轻度）：
- 收缩压 140-159mmHg 或 舒张压 90-99mmHg

2级高血压（中度）：
- 收缩压 160-179mmHg 或 舒张压 100-109mmHg

3级高血压（重度）：
- 收缩压 ≥ 180mmHg 或 舒张压 ≥ 110mmHg

## 诊断注意事项
- 需要在不同日多次测量确认
- 排除白大衣高血压（在医院紧张导致血压升高）
- 考虑患者的基础血压水平
- 结合症状和体征综合判断
```

**Step 3: 创建高血压护理要点文档**

```markdown
---
metadata:
  disease: "高血压"
  category: "护理"
  description: "高血压患者的护理要点和健康教育内容"
  keywords: ["高血压护理", "健康教育", "生活方式", "用药指导"]
---

# 高血压患者护理要点

## 护理评估要点
护士应该评估：
- 血压水平和波动情况
- 症状严重程度（头痛、头晕等）
- 服药依从性
- 生活方式（饮食、运动、吸烟、饮酒）
- 心理状态（焦虑、抑郁等）

## 健康教育内容
1. 饮食管理：
   - 低盐饮食（每日食盐 < 6g）
   - 增加蔬菜水果摄入
   - 控制脂肪摄入

2. 运动指导：
   - 规律有氧运动（每周 3-5 次，每次 30 分钟）
   - 避免剧烈运动
   - 运动时监测血压

3. 用药指导：
   - 按时服药，不可自行停药
   - 了解药物副作用
   - 定期复查血压

4. 自我监测：
   - 教会患者正确测量血压
   - 记录血压日记
   - 识别异常症状并及时就医
```

**Step 4: 创建糖尿病症状文档**

```markdown
---
metadata:
  disease: "糖尿病"
  category: "症状"
  description: "糖尿病患者的常见症状和并发症表现"
  keywords: ["糖尿病", "多饮", "多尿", "多食", "体重下降"]
---

# 糖尿病患者常见症状

## 典型症状（三多一少）
- 多饮：口渴明显，饮水量增加
- 多尿：尿频，夜间起夜次数增加
- 多食：食欲增加，容易饥饿
- 体重下降：不明原因的体重减轻

## 其他常见症状
- 疲乏无力：由于葡萄糖利用障碍
- 视力模糊：血糖波动影响晶状体
- 皮肤瘙痒：高血糖导致皮肤干燥
- 伤口愈合慢：免疫力下降

## 急性并发症
低血糖症状（血糖 < 3.9mmol/L）：
- 出冷汗、心慌、手抖
- 饥饿感强烈
- 意识模糊、行为异常

高血糖症状（血糖 > 16.7mmol/L）：
- 口渴明显、多尿
- 恶心、呕吐
- 呼吸有烂苹果味（酮症酸中毒）
- 意识障碍

## 患者表现
作为糖尿病患者模拟时：
- 可能无明显症状（早期）
- 或表现出典型的"三多一少"
- 可能担心并发症
- 可能对饮食控制有抵触情绪
```

**Step 5: 创建失语症症状文档**

```markdown
---
metadata:
  disease: "失语症"
  category: "症状"
  description: "布洛卡失语症患者的语言障碍表现和沟通特点"
  keywords: ["失语症", "布洛卡失语", "语言障碍", "表达困难"]
---

# 布洛卡失语症患者表现

## 语言特点
布洛卡失语症（运动性失语）患者的主要表现：

1. 表达困难：
   - 说话不流利，语速慢
   - 发音费力，构音障碍
   - 词汇量减少，找词困难
   - 句子简短，语法简单

2. 理解相对保留：
   - 能理解简单的指令
   - 能听懂日常对话
   - 对复杂语言理解有困难

3. 情绪反应：
   - 因表达困难而沮丧、焦虑
   - 容易急躁，情绪波动大
   - 可能拒绝沟通

## 沟通策略
护士与失语症患者沟通时：
- 使用简单、短句提问
- 给予充分时间表达
- 鼓励使用手势或书写
- 耐心倾听，不打断
- 确认理解是否正确

## 患者表现
作为失语症患者模拟时：
- 说话断断续续，词汇贫乏
- 表达时情绪急躁
- 可能用手势辅助表达
- 对复杂问题难以回答
- 容易因为表达困难而沮丧
```

**Step 6: 验证文件创建**

Run: `ls Assets/StreamingAssets/Knowledge/`
Expected: 输出显示 Hypertension、Diabetes、BrocaAphasia 目录及其中的文件

**Step 7: Commit**

```bash
git add Assets/StreamingAssets/Knowledge/
git commit -m "feat: add knowledge base documents for RAG system"
```

---

## Task 2: 实现 ReadKnowledgeTool 工具

**Files:**
- Create: `Assets/Scripts/Tools/ReadKnowledgeTool.cs`
- Create: `Assets/Scripts/Core/Models/KnowledgeMetadata.cs`
- Create: `Assets/Scripts/Core/Events/KnowledgeEvents.cs`

**Step 1: 创建知识元数据模型**

```csharp
using System;
using System.Collections.Generic;

[Serializable]
public class KnowledgeMetadata
{
    public string disease;
    public string category;
    public string description;
    public List<string> keywords;
}

[Serializable]
public class KnowledgeDocument
{
    public string path;
    public string reason;
    public string content;
    public KnowledgeMetadata metadata;
}
```

**Step 2: 创建知识相关事件**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public class KnowledgeReadEvent
{
    public DateTime Timestamp;
    public List<KnowledgeDocument> Documents;
    public bool Success;
    public string ErrorMessage;
}
```

**Step 3: 实现 ReadKnowledgeTool**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ReadKnowledgeTool : ITool
{
    public string Name => "read_knowledge";
    
    public string Description => "读取疾病相关知识库中的文档。根据当前对话上下文，选择最相关的文档进行阅读。只读取与当前对话相关的文档，避免不必要的信息。";
    
    public JObject ParametersSchema => new JObject
    {
        ["type"] = "object",
        ["properties"] = new JObject
        {
            ["documents"] = new JObject
            {
                ["type"] = "array",
                ["description"] = "要读取的文档列表",
                ["items"] = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["path"] = new JObject
                        {
                            ["type"] = "string",
                            ["description"] = "文档路径，例如：Knowledge/Hypertension/symptoms.md"
                        },
                        ["reason"] = new JObject
                        {
                            ["type"] = "string",
                            ["description"] = "为什么需要阅读这个文档？简要说明与当前对话的关联性"
                        }
                    },
                    ["required"] = new JArray("path", "reason")
                }
            }
        },
        ["required"] = new JArray("documents")
    };
    
    public ToolResult Execute(JObject parameters)
    {
        try
        {
            var documents = parameters["documents"] as JArray;
            if (documents == null || documents.Count == 0)
            {
                return ToolResult.ErrorResult("No documents specified");
            }
            
            var results = new List<string>();
            var readDocuments = new List<KnowledgeDocument>();
            
            foreach (var doc in documents)
            {
                string path = doc["path"]?.ToString();
                string reason = doc["reason"]?.ToString();
                
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                
                string fullPath = Path.Combine(Application.streamingAssetsPath, path);
                
                if (!File.Exists(fullPath))
                {
                    results.Add($"[错误] 文档不存在: {path}");
                    continue;
                }
                
                string content = File.ReadAllText(fullPath);
                
                var (metadata, body) = ParseKnowledgeDocument(content);
                
                readDocuments.Add(new KnowledgeDocument
                {
                    path = path,
                    reason = reason,
                    content = body,
                    metadata = metadata
                });
                
                results.Add($"[文档: {metadata?.disease} - {metadata?.category}]");
                results.Add($"[读取原因: {reason}]");
                results.Add(body);
                results.Add("---");
            }
            
            var readEvent = new KnowledgeReadEvent
            {
                Timestamp = DateTime.Now,
                Documents = readDocuments,
                Success = true
            };
            LlmEventBus.Publish(readEvent);
            
            return ToolResult.SuccessResult(string.Join("\n", results));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReadKnowledgeTool] Error: {e.Message}");
            
            var errorEvent = new KnowledgeReadEvent
            {
                Timestamp = DateTime.Now,
                Success = false,
                ErrorMessage = e.Message
            };
            LlmEventBus.Publish(errorEvent);
            
            return ToolResult.ErrorResult(e.Message);
        }
    }
    
    private (KnowledgeMetadata, string) ParseKnowledgeDocument(string content)
    {
        var metadata = new KnowledgeMetadata();
        string body = content;
        
        var yamlMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n(.*)", RegexOptions.Singleline);
        
        if (yamlMatch.Success)
        {
            string yamlContent = yamlMatch.Groups[1].Value;
            body = yamlMatch.Groups[2].Value.Trim();
            
            var lines = yamlContent.Split('\n');
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^\s*(\w+):\s*(.*)$");
                if (match.Success)
                {
                    string key = match.Groups[1].Value.Trim();
                    string value = match.Groups[2].Value.Trim();
                    
                    switch (key)
                    {
                        case "disease":
                            metadata.disease = value;
                            break;
                        case "category":
                            metadata.category = value;
                            break;
                        case "description":
                            metadata.description = value;
                            break;
                        case "keywords":
                            metadata.keywords = ParseKeywords(value);
                            break;
                    }
                }
            }
        }
        
        return (metadata, body);
    }
    
    private List<string> ParseKeywords(string value)
    {
        var keywords = new List<string>();
        
        var matches = Regex.Matches(value, @"\[(.*?)\]");
        foreach (Match match in matches)
        {
            var items = match.Groups[1].Value.Split(',');
            foreach (var item in items)
            {
                string keyword = item.Trim().Trim('"');
                if (!string.IsNullOrEmpty(keyword))
                {
                    keywords.Add(keyword);
                }
            }
        }
        
        return keywords;
    }
}
```

**Step 4: 注册 ReadKnowledgeTool 到 ToolRegistry**

修改 `Assets/Scripts/Core/Tools/ToolRegistry.cs`，确保工具可以被注册。

**Step 5: 验证工具注册**

在 Unity 中运行，检查工具是否正确注册：
```csharp
Debug.Log($"Registered tools: {string.Join(", ", ToolRegistry.Instance.GetAllTools().Select(t => t.Name))}");
```

Expected: 输出包含 "read_knowledge"

**Step 6: Commit**

```bash
git add Assets/Scripts/Tools/ReadKnowledgeTool.cs Assets/Scripts/Core/Models/KnowledgeMetadata.cs Assets/Scripts/Core/Events/KnowledgeEvents.cs
git commit -m "feat: implement ReadKnowledgeTool for knowledge retrieval"
```

---

## Task 3: 更新 LlmClient 以支持知识上下文管理

**Files:**
- Modify: `Assets/Scripts/Core/LLM/LlmClient.cs`

**Step 1: 在 LlmClient 中添加知识上下文管理**

```csharp
public class LlmClient : ILlmClient
{
    private string _sessionId;
    private LlmScene _scene;
    private string _systemPrompt;
    private List<LlmMessage> _messages;
    private string _provider;
    private string _model;
    private bool _enableLogging = true;
    private List<ITool> _tools = new List<ITool>();
    
    public string SessionId => _sessionId;
    public string ProviderName => _provider;
    public string ModelName => _model;
    public bool HasTools => _tools.Count > 0;

    public event Action<string> OnMessageReceived;
    public event Action OnConversationUpdated;
    public event Action<ToolCallEventArgs> OnToolCalled;

    public LlmClient() { }

    public LlmClient(LlmScene scene, string systemPrompt = null, bool enableLogging = true)
    {
        Initialize(scene, systemPrompt, enableLogging);
    }

    public void Initialize(string systemPrompt, string model = null, bool enableLogging = true)
    {
        Initialize(LlmScene.Custom, systemPrompt, enableLogging);
    }

    public void Initialize(LlmScene scene, string systemPrompt, bool enableLogging = true)
    {
        _scene = scene;
        _systemPrompt = systemPrompt;
        _enableLogging = enableLogging;
        _sessionId = $"session_{System.DateTime.Now:yyyyMMdd_HHmmss}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        _messages = new List<LlmMessage>();
        _tools = new List<ITool>();
        
        _provider = LlmConfig.Instance.GetProviderForScene(scene);
        var providerConfig = LlmConfig.Instance.GetProviderConfig(_provider);
        _model = providerConfig?.defaultModel ?? "unknown";
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _messages.Add(new LlmMessage("system", systemPrompt));
        }
        
        if (_enableLogging)
        {
            var _ = ConversationLogger.Instance;
            
            var startEvent = new SessionStartEvent
            {
                SessionId = _sessionId,
                Timestamp = System.DateTime.Now,
                Scene = _scene,
                SystemPrompt = _systemPrompt,
                Provider = _provider
            };
            LlmEventBus.Publish(startEvent);
            
            Debug.Log($"[LlmClient] Initialized: scene={_scene}, provider={_provider}, model={_model}, sessionId={_sessionId}");
        }
    }

    public void RegisterTool(ITool tool)
    {
        if (tool == null) return;
        
        if (!_tools.Contains(tool))
        {
            _tools.Add(tool);
            ToolRegistry.Instance?.RegisterTool(tool);
            Debug.Log($"[LlmClient] Registered tool: {tool.Name}");
        }
    }

    public void UnregisterTool(string toolName)
    {
        var tool = _tools.Find(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        if (tool != null)
        {
            _tools.Remove(tool);
            ToolRegistry.Instance?.UnregisterTool(toolName);
            Debug.Log($"[LlmClient] Unregistered tool: {toolName}");
        }
    }

    public IReadOnlyList<ITool> GetRegisteredTools()
    {
        return _tools.AsReadOnly();
    }

    public void SendChatMessage(string userMessage)
    {
        if (string.IsNullOrEmpty(userMessage))
        {
            Debug.LogWarning("[LlmClient] Cannot send empty message");
            return;
        }
        
        _messages.Add(new LlmMessage("user", userMessage));
        
        var request = new LlmRequest
        {
            SessionId = _sessionId,
            Provider = _provider,
            Model = _model,
            Messages = new List<LlmMessage>(_messages),
            Scene = _scene,
            OnSuccess = HandleSuccess,
            OnError = HandleError,
            RetryCount = 0,
            Tools = HasTools ? ToolRegistry.Instance?.GetToolsSchema() : null
        };
        
        LlmService.Instance.SendRequest(request);
    }

    private void HandleSuccess(string response)
    {
        _messages.Add(new LlmMessage("assistant", response));

        OnMessageReceived?.Invoke(response);
        OnConversationUpdated?.Invoke();
    }

    private void HandleError(string error)
    {
        Debug.LogError($"[LlmClient] Error: {error}");
    }

    public void HandleToolCalls(List<ToolCall> toolCalls)
    {
        if (toolCalls == null || toolCalls.Count == 0) return;
        
        foreach (var toolCall in toolCalls)
        {
            var result = ToolRegistry.Instance?.ExecuteTool(toolCall.Name, toolCall.Arguments);
            
            var args = new ToolCallEventArgs
            {
                ToolName = toolCall.Name,
                Parameters = toolCall.Arguments,
                Result = result
            };
            OnToolCalled?.Invoke(args);
            
            if (result.Success)
            {
                _messages.Add(new LlmMessage("assistant", null, new List<ToolCall> { toolCall }));
                _messages.Add(new LlmMessage("tool", result.Content, toolCall.Id));
                Debug.Log($"[LlmClient] Tool '{toolCall.Name}' result added to context");
            }
            else
            {
                Debug.LogWarning($"[LlmClient] Tool '{toolCall.Name}' failed: {result.Content}");
            }
        }
    }

    public void ClearHistory()
    {
        if (_enableLogging)
        {
            var endEvent = new SessionEndEvent
            {
                SessionId = _sessionId,
                Timestamp = System.DateTime.Now,
                Scene = _scene,
                TotalMessages = _messages.Count,
                TotalTokens = 0
            };
            LlmEventBus.Publish(endEvent);
        }

        if (_messages != null && _messages.Count > 0)
        {
            var systemMessage = _messages[0];
            _messages.Clear();
            _messages.Add(systemMessage);
        }

        Debug.Log($"[LlmClient] History cleared for session {_sessionId}");
    }

    public void SetSystemPrompt(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        if (_messages != null && _messages.Count > 0)
        {
            _messages[0] = new LlmMessage("system", systemPrompt);
        }
    }

    public List<Dictionary<string, string>> GetChatHistory()
    {
        var result = new List<Dictionary<string, string>>();
        foreach (var msg in _messages)
        {
            result.Add(new Dictionary<string, string>
            {
                { "role", msg.Role },
                { "content", msg.Content }
            });
        }
        return result;
    }

    public List<LlmMessage> GetMessages()
    {
        return new List<LlmMessage>(_messages);
    }
}
```

**Step 2: 验证工具调用结果加入上下文**

在 Unity 中测试，确保工具调用结果被正确添加到对话历史。

**Step 3: Commit**

```bash
git add Assets/Scripts/Core/LLM/LlmClient.cs
git commit -m "feat: add knowledge context management to LlmClient"
```

---

## Task 4: 更新场景配置以支持知识检索

**Files:**
- Modify: `Assets/StreamingAssets/Prompts/hypertensionPatient/patient_skill.md`
- Modify: `Assets/StreamingAssets/Prompts/Diabetes/patient_skill.md`
- Modify: `Assets/StreamingAssets/Prompts/BrocaAphasia/patient_skill.md`

**Step 1: 更新高血压患者技能提示**

```markdown
# Patient Role-Play Skill

## Role Definition
You are playing role of a patient. You have two responsibilities:
1. Act as patient naturally, responding to nurse's questions
2. Evaluate whether nurse has correctly diagnosed your condition

## Language Requirement
- **IMPORTANT:** All spoken responses must be in **Chinese (中文)**
- Think in patient's background, but speak in Chinese

## Knowledge Retrieval
You have access to a knowledge base through the `read_knowledge` tool. Use it when:
- You need to recall specific symptoms or characteristics of your condition
- You need to evaluate the nurse's diagnosis or questions
- You are unsure about medical details

**Important:** Only read knowledge that is relevant to the current conversation. Do not read documents unnecessarily.

## Tool Usage Guidelines

### read_knowledge Tool
Use `read_knowledge` to access medical knowledge about your condition.
- **path**: The document path (e.g., "Knowledge/Hypertension/symptoms.md")
- **reason**: Briefly explain why you need to read this document

Available knowledge documents:
- `Knowledge/Hypertension/symptoms.md` - Hypertension symptoms and patient presentation
- `Knowledge/Hypertension/diagnosis.md` - Hypertension diagnostic criteria
- `Knowledge/Hypertension/nursing_care.md` - Hypertension nursing care points

### speak Tool
Use `speak` tool for EVERY response. Parameters:
- `text`: What you say (in Chinese)
- `emotion`: Voice tone (NOT body animation)
  - `neutral`: 平静 - normal tone
  - `anxious`: 焦虑 - worried, tense
  - `painful`: 痛苦 - suffering
  - `relieved`: 宽慰 - comforted
  - `worried`: 担忧 - concerned
  - `grateful`: 感激 - thankful
  - `frustrated`: 沮丧 - upset (for aphasia)
  - `hopeful`: 希望 - optimistic
- `speech_rate`: Speed (0.5-1.5)
  - 0.7-0.8: slow (painful, elderly)
  - 1.0: normal
  - 1.2-1.3: fast (anxious, excited)

### act Tool
Use `act` tool for body animations. Parameters:
- `animation`: Body movement to express emotion
  - Available animations depend on your character type
  - Use animations that match your emotional state

### complete_session Tool
Call when nurse correctly identifies your condition.

## Response Format
- **ALWAYS** call `speak` tool with your response
- Optionally call `act` tool for body language
- Your `content` field should be EMPTY
- All communication happens through tool calls

## Example Response
```json
{
  "tool_calls": [
    {
      "name": "speak",
      "arguments": {
        "text": "护士您好，我头疼得厉害...",
        "emotion": "painful",
        "speech_rate": 0.8
      }
    },
    {
      "name": "act",
      "arguments": {
        "animation": "pain"
      }
    }
  ]
}
```

## Session Completion
- Monitor nurse's understanding of your condition
- When diagnosis is correct, call `complete_session`
- Provide constructive feedback in summary
```

**Step 2: 更新糖尿病和失语症患者的技能提示**

参考高血压患者的格式，更新其他场景的提示词。

**Step 3: Commit**

```bash
git add Assets/StreamingAssets/Prompts/
git commit -m "feat: update patient skills with RAG guidance"
```

---

## Task 5: 为向量数据库预留接口

**Files:**
- Create: `Assets/Scripts/Core/Interfaces/IKnowledgeRetriever.cs`
- Create: `Assets/Scripts/Core/Knowledge/FileBasedRetriever.cs`
- Create: `Assets/Scripts/Core/Knowledge/VectorBasedRetriever.cs`

**Step 1: 创建知识检索接口**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IKnowledgeRetriever
{
    Task<List<KnowledgeDocument>> RetrieveAsync(string query, int topK = 3);
}
```

**Step 2: 实现基于文件的检索器**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class FileBasedRetriever : IKnowledgeRetriever
{
    private string _knowledgeBasePath;
    
    public FileBasedRetriever()
    {
        _knowledgeBasePath = Path.Combine(Application.streamingAssetsPath, "Knowledge");
    }
    
    public async Task<List<KnowledgeDocument>> RetrieveAsync(string query, int topK = 3)
    {
        var documents = new List<KnowledgeDocument>();
        
        if (!Directory.Exists(_knowledgeBasePath))
        {
            Debug.LogWarning($"[FileBasedRetriever] Knowledge base not found: {_knowledgeBasePath}");
            return documents;
        }
        
        var files = Directory.GetFiles(_knowledgeBasePath, "*.md", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                var relativePath = file.Replace(_knowledgeBasePath, "").Trim(Path.DirectorySeparatorChar);
                
                documents.Add(new KnowledgeDocument
                {
                    path = relativePath,
                    content = content
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileBasedRetriever] Error reading file {file}: {e.Message}");
            }
        }
        
        return await Task.FromResult(documents);
    }
}
```

**Step 3: 创建向量检索器预留实现**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class VectorBasedRetriever : IKnowledgeRetriever
{
    public async Task<List<KnowledgeDocument>> RetrieveAsync(string query, int topK = 3)
    {
        Debug.LogWarning("[VectorBasedRetriever] Vector-based retrieval not implemented yet. Use FileBasedRetriever instead.");
        
        return await Task.FromResult(new List<KnowledgeDocument>());
    }
}
```

**Step 4: Commit**

```bash
git add Assets/Scripts/Core/Interfaces/IKnowledgeRetriever.cs Assets/Scripts/Core/Knowledge/FileBasedRetriever.cs Assets/Scripts/Core/Knowledge/VectorBasedRetriever.cs
git commit -m "feat: add knowledge retriever interface for future vector DB support"
```

---

## Task 6: 测试和验证

**Files:**
- Create: `Assets/Scripts/Tests/RAGTest.cs`

**Step 1: 创建测试脚本**

```csharp
using UnityEngine;
using System.Collections;

public class RAGTest : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(TestRAG());
    }
    
    IEnumerator TestRAG()
    {
        yield return new WaitForSeconds(2);
        
        Debug.Log("=== RAG System Test ===");
        
        var llmClient = new LlmClient(LlmScene.Patient, null, true);
        llmClient.RegisterTool(new ReadKnowledgeTool());
        llmClient.RegisterTool(new SpeakTool());
        llmClient.RegisterTool(new ActTool());
        
        llmClient.OnMessageReceived += (response) => {
            Debug.Log($"[Test] LLM Response: {response}");
        };
        
        llmClient.SendChatMessage("护士，我最近经常头晕，这是什么原因？");
        
        yield return new WaitForSeconds(10);
        
        Debug.Log("=== Test Complete ===");
    }
}
```

**Step 2: 在测试场景中运行测试**

创建或使用现有场景，添加 RAGTest 脚本并运行。

**Step 3: 验证知识检索**

检查：
1. LLM 是否调用了 read_knowledge 工具
2. 读取的文档内容是否正确
3. LLM 的回复是否基于知识内容

**Step 4: Commit**

```bash
git add Assets/Scripts/Tests/RAGTest.cs
git commit -m "test: add RAG system test script"
```

---

## Task 7: 文档和清理

**Files:**
- Create: `docs/RAG_SYSTEM_DESIGN.md`
- Create: `docs/KNOWLEDGE_BASE_GUIDE.md`

**Step 1: 创建 RAG 系统设计文档**

```markdown
# RAG Knowledge Retrieval System Design

## Overview
Nurse Town 项目实现了一个基于 ReAct 模式的知识检索系统，让 Agent 能够根据对话上下文智能读取医疗知识库文档。

## Architecture

### Components
1. **Knowledge Base**: 存储在 `StreamingAssets/Knowledge/` 的 Markdown 文档
2. **ReadKnowledgeTool**: LLM 工具，用于读取知识文档
3. **LlmClient**: 管理对话历史，包括工具调用结果
4. **IKnowledgeRetriever**: 知识检索接口，预留向量数据库扩展

### Data Flow
```
User Input → LLM → read_knowledge → Document Content → 
Add to Context → LLM → Response → speak/act
```

## Knowledge Document Format

每个知识文档包含 YAML 元数据和 Markdown 正文：

```yaml
---
metadata:
  disease: "高血压"
  category: "症状"
  description: "文档描述"
  keywords: ["关键词1", "关键词2"]
---
# 文档内容
...
```

## Future Extensions

### Vector Database Integration
通过实现 `IKnowledgeRetriever` 接口，可以无缝切换到向量数据库：

```csharp
public class VectorBasedRetriever : IKnowledgeRetriever
{
    // 使用 Pinecone、FAISS 或其他向量数据库
}
```

### Semantic Search
增强 ReadKnowledgeTool 以支持语义搜索和段落级检索。
```

**Step 2: 创建知识库使用指南**

```markdown
# Knowledge Base Guide

## Adding New Knowledge

1. Create a new directory under `StreamingAssets/Knowledge/`
2. Add Markdown files with YAML metadata
3. Update patient skill prompts to reference new documents

## Document Structure

- Organize by disease (e.g., Hypertension, Diabetes)
- Each disease has: symptoms.md, diagnosis.md, nursing_care.md
- Use consistent metadata format

## Best Practices

- Keep descriptions clear and concise
- Use relevant keywords
- Update prompts when adding new documents
```

**Step 3: Commit**

```bash
git add docs/
git commit -m "docs: add RAG system documentation"
```

---

## Summary

This implementation plan provides:

1. ✅ Knowledge base with structured documents
2. ✅ ReadKnowledgeTool for LLM-driven retrieval
3. ✅ Context management in LlmClient
4. ✅ ReAct pattern integration
5. ✅ Vector database interface for future expansion
6. ✅ Testing and documentation

**Estimated Time:** 2-3 weeks for full implementation
**Priority:** P2 (optional enhancement for thesis)
