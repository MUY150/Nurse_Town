# Emotion System Refactor

## 概述
本次重构统一了emotion系统，解决了以下问题：
- 单例组件懒加载缺失
- 配置文件命名冲突
- Emotion Code与Tool冲突
- 动画系统接口不统一

## 主要变更

### 1. 单例模式统一
所有Manager组件现在继承Singleton<T>，实现自动懒加载：
- TTSManager
- Audio2FaceManager
- PatientDialogueController

### 2. 配置文件命名规范
统一使用角色ID作为配置文件名：
- hypertensionPatient.json
- sitting.json
- 移除了GetDefaultConfig方法

### 3. 移除Emotion Code机制
完全移除emotion code机制，统一使用Tool调用方式：
- 移除了GetEmotionInstructions方法
- 移除了HandleEmotionCode方法
- 移除了TTSManager.UpdateAnimation中的emotion code提取

### 4. 统一动画接口
创建了ICharacterAnimation接口：
- EmotionController实现ICharacterAnimation（Timeline系统）
- CharacterAnimationController实现ICharacterAnimation（Animator系统）
- 两套系统可以同时使用

### 5. 血液效果触发集中
血液效果触发逻辑集中到AnimationService：
- 移除了PatientDialogueController中的硬编码触发
- 通过AnimationService.TriggerBloodEffect()统一触发

## 使用示例

### 使用Tool调用动画
```csharp
// LLM调用
act(animation: "pain")

// 内部实现
AnimationService.Instance.PlayAnimation("pain");
```

### 使用EmotionController（面部表情）
```csharp
ICharacterAnimation controller = GetComponent<EmotionController>();
controller.PlayByEmotionCode(1);
```

### 使用CharacterAnimationController（身体动作）
```csharp
ICharacterAnimation controller = GetComponent<CharacterAnimationController>();
controller.PlayAnimation("pain");
```

## 配置文件示例

### hypertensionPatient.json
```json
{
  "characterType": "hypertension",
  "maxEmotionCode": 10,
  "emotionMappings": [...],
  "namedAnimations": {...}
}
```

### sitting.json
```json
{
  "characterType": "sitting",
  "maxEmotionCode": 4,
  "emotionMappings": [...],
  "namedAnimations": {...}
}
```

## 已知问题和待解决

### 1. 配置文件加载不一致
**问题**：CharacterAnimationController加载的是`standing`配置，而不是`hypertensionPatient`配置。

**日志证据**：
```
[CharacterAnimationController] Loaded config: standing, maxEmotionCode: 10
```

**原因**：CharacterAnimationController有自己的配置加载逻辑，没有统一使用AnimationService。

**解决方案**：让CharacterAnimationController使用AnimationService的配置，或者移除CharacterAnimationController的独立配置加载。

### 2. Animator参数不存在
**问题**：Animator控制器中没有`Motion`参数。

**日志证据**：
```
[WARNING] Parameter 'Motion' does not exist.
```

**原因**：硬编码的参数名不适用于所有角色。

**解决方案**：在Animator控制器中添加`Motion`参数，或者在CharacterAnimationController中添加可配置的参数名。

### 3. 等待时间计算错误
**问题**：audioClip.length计算错误，显示为44739.74秒。

**日志证据**：
```
[WARNING] [TTSManager] Wait time too long: 44739.74, capping at 60 seconds
```

**原因**：可能是类型转换或单位问题。

**解决方案**：检查audioClip.length的数据类型和单位，确保正确计算。

### 4. Audio2FaceManager缺失
**问题**：场景中没有Audio2FaceManager组件。

**日志证据**：
```
[WARNING] Audio2FaceManager not found in the scene. Audio2Face integration disabled.
```

**解决方案**：在场景中添加Audio2FaceManager组件，或者移除TTSManager对Audio2FaceManager的依赖。
