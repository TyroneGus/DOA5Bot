namespace BTree;

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DOA5Info;
using static DOA5Info.GameInfo;

// 基础节点类
[JsonConverter(typeof(NodeJsonConverter))]
public abstract class Node
{
    public abstract NodeState Evaluate();
    public abstract void Serialize(JsonWriter writer, JsonSerializer serializer);
}

public class AttackNode : Node
{
    public override NodeState Evaluate() => NodeState.Success;

    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("AttackNode");
        writer.WriteEndObject();
    }
}

public class DefendNode : Node
{
    public override NodeState Evaluate() => NodeState.Success;

    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("DefendNode");
        writer.WriteEndObject();
    }
}

public class MoveNode : Node
{
    public override NodeState Evaluate() => NodeState.Success;

    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("MoveNode");
        writer.WriteEndObject();
    }
}

public class PerformMoveActionNode : Node
{
    private NodeState state;
    public string MoveName { get; set; }
    public PerformMoveActionNode(string moveName) => MoveName = moveName;

    // public override NodeState Evaluate() => NodeState.Success;
    public override NodeState Evaluate()
    {
        // int lastFrame = GameInfo.Player.CurrentMoveFrame;
        Console.WriteLine("enter perform move action node");
        if (state != NodeState.Running)
        {
            state = NodeState.Running;
            // 如果执行输入的动作不成功,则返回失败
            if (!ComboParser.ExecuteCombo(MoveName))
            {
                state = NodeState.Failure;
                // return state;
            }
            else
            {
                state = NodeState.Success;
                // return state;
            }
        }

        Console.WriteLine("exit perform move action node, node state: " + state);
        return state;
    }

    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("PerformMoveActionNode");
        writer.WritePropertyName("MoveName");
        writer.WriteValue(MoveName);
        writer.WriteEndObject();
    }
}

public abstract class CompositeNode : Node
{
    public List<Node> Children { get; set; }

    public CompositeNode(List<Node> children) => Children = children;
}

public class SelectorNode : CompositeNode
{
    public SelectorNode(List<Node> children) : base(children)
    {
    }


    public override NodeState Evaluate()
    {
        foreach (var child in Children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Success:
                    return NodeState.Success;
                case NodeState.Running:
                    return NodeState.Running;
                case NodeState.Failure:
                    continue;
            }
        }

        return NodeState.Failure;
    }

    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("SelectorNode");
        writer.WritePropertyName("Children");
        writer.WriteStartArray();
        foreach (var child in Children)
        {
            child.Serialize(writer, serializer);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

public class SequenceNode : CompositeNode
{
    public SequenceNode(List<Node> children) : base(children)
    {
    }

    public override NodeState Evaluate()
    {
        bool isRunning = false;

        foreach (var child in Children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Success:
                    continue;
                case NodeState.Running:
                    isRunning = true;
                    continue;
                case NodeState.Failure:
                    return NodeState.Failure;
            }
        }

        return isRunning ? NodeState.Running : NodeState.Success;
    }

    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("SequenceNode");
        writer.WritePropertyName("Children");
        writer.WriteStartArray();
        foreach (var child in Children)
        {
            child.Serialize(writer, serializer);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

public enum NodeState
{
    Failure,
    Success,
    Running
}

// Json转换器
public class NodeJsonConverter : JsonConverter<Node>
{
    public override Node ReadJson(JsonReader reader, Type objectType, Node existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        string nodeType = obj["NodeType"].ToString();
        Node node = nodeType switch
        {
            "AttackNode" => new AttackNode(),
            "DefendNode" => new DefendNode(),
            "MoveNode" => new MoveNode(),
            // "ConditionNode" => new ConditionNode(() => true), // 示例条件处理
            // "ConditionNode" => new ConditionNode(), // 示例条件处理
            "ConditionNode" => new ConditionNode
            {
                ComplexConditions = obj["ComplexConditions"].ToObject<List<ComplexCondition>>(serializer)
            },
            "SelectorNode" => new SelectorNode(new List<Node>()),
            "SequenceNode" => new SequenceNode(new List<Node>()),
            "PerformMoveActionNode" => new PerformMoveActionNode(obj["MoveName"].ToString()),
            _ => throw new InvalidOperationException($"Unknown node type: {nodeType}")
        };

        // 如果节点有子节点，则继续反序列化
        if (node is CompositeNode compositeNode && obj["Children"] != null)
        {
            compositeNode.Children = obj["Children"].ToObject<List<Node>>(serializer);
        }
        /*else if (node is ConditionNode conditionNode)
        {
            // 处理条件节点的反序列化
            string condition = obj["Condition"].ToString();
            // conditionNode.Condition = () => EvaluateCondition(condition);
            var conditions = JsonConvert.DeserializeObject<List<Condition>>(condition);
            // string jsonString = "[{\"Name\":\"Age\",\"TypedValue\":{\"Value\":18,\"Type\":\"System.Int32\"}},{\"Name\":\"Gender\",\"TypedValue\":{\"Value\":\"Male\",\"Type\":\"System.String\"}}]";
            /*foreach (Condition cond in conditions)
            {
                // string typedValueJson = obj["TypedValue"].ToString();
                string typedValueJson = JsonConvert.SerializeObject(cond.TypedValue);
                cond.TypedValue = JsonConvert.DeserializeObject<TypedValue>(typedValueJson);
                // cond.TypedValue = Condition.ParseTypedValue(typedValueJson);
            }#1#
            conditionNode.Conditions = conditions;
        }*/

        return node;
    }

    public override void WriteJson(JsonWriter writer, Node value, JsonSerializer serializer)
    {
        value.Serialize(writer, serializer);
    }

    // private bool EvaluateCondition(string condition)
    // {
    //     // 在这里解析和评估条件表达式
    //     // 这部分需要具体的实现逻辑来解析字符串形式的条件
    //     return false; // 仅作占位
    // }
}

/*public class TypedValue
{
    public string Type { get; set; }
    public object Value { get; set; }
}

public class Condition
{
    public string Property { get; set; }
    public string Operator { get; set; }
    public TypedValue TypedValue { get; set; }

    public T GetValueAs<T>()
    {
        return (T)Convert.ChangeType(TypedValue.Value, typeof(T));
    }

    // 反序列化时，解析 TypedValue 并转换为对应类型
    public static TypedValue ParseTypedValue(JToken token)
    {
        var typeToken = token["Type"];
        var valueToken = token["Value"];

        Type type = Type.GetType(typeToken.ToString());
        object value = Convert.ChangeType(valueToken.ToString(), type);

        return new TypedValue
        {
            Type = type.FullName,
            Value = value
        };
    }
}*/
public class ComplexCondition
{
    public List<Condition> Conditions { get; set; } = new List<Condition>();
    public string LogicalOperator { get; set; } = "AND"; // 可以是 "AND" 或 "OR"

    public bool Evaluate()
    {
        if (LogicalOperator == "AND")
        {
            return Conditions.All(c => EvaluateCondition(c));
        }
        else if (LogicalOperator == "OR")
        {
            return Conditions.Any(c => EvaluateCondition(c));
        }
        throw new InvalidOperationException("Invalid logical operator");
    }

    private bool EvaluateCondition(Condition condition)
    {
        // 这里使用之前实现的 EvaluateCondition 方法
        object propertyValue = GetPropertyValue(condition.Property);
        return EvaluateCondition(propertyValue, condition.Operator, condition.Value);
    }

    private static bool EvaluateCondition(object propertyValue, string operatorSymbol, object conditionValue)
    {
        // 确保两个值都转换为相同的类型（选择范围更大的类型）
        Type commonType = GetCommonType(propertyValue.GetType(), conditionValue.GetType());
        var convertedPropertyValue = Convert.ChangeType(propertyValue, commonType);
        var convertedConditionValue = Convert.ChangeType(conditionValue, commonType);

        if (convertedPropertyValue is IComparable comparablePropertyValue &&
            convertedConditionValue is IComparable comparableConditionValue)
        {
            int comparisonResult = comparablePropertyValue.CompareTo(comparableConditionValue);
            return operatorSymbol switch
            {
                "<=" => comparisonResult <= 0,
                "<" => comparisonResult < 0,
                ">=" => comparisonResult >= 0,
                ">" => comparisonResult > 0,
                "==" => comparisonResult == 0,
                "!=" => comparisonResult != 0,
                _ => throw new InvalidOperationException("Unsupported operator")
            };
        }

        throw new InvalidOperationException("Values are not comparable");
    }

    private static Type GetCommonType(Type type1, Type type2)
    {
        if (type1 == type2) return type1;
        if (type1 == typeof(double) || type2 == typeof(double)) return typeof(double);
        if (type1 == typeof(float) || type2 == typeof(float)) return typeof(float);
        if (type1 == typeof(long) || type2 == typeof(long)) return typeof(long);
        if (type1 == typeof(int) || type2 == typeof(int)) return typeof(int);
        if (type1 == typeof(short) || type2 == typeof(short)) return typeof(short);
        return typeof(int); // 默认使用int
    }
    private object GetPropertyValue(string propertyName)
    {
        Console.WriteLine($"GetPropertyValue: {propertyName}");
        return propertyName switch
        {
            "PX_Distance" => PX_Distance,
            "PX_TotalActiveFrames" => PX_TotalActiveFrames,
            
            "Player.Airborne" => Player.Airborne,
            "Player.ComboCounter" => Player.ComboCounter,
            "Player.CurrentCharacter" => Player.CurrentCharacter,
            "Player.CurrentMove" => Player.CurrentMove,
            "Player.CurrentMoveFrame" => Player.CurrentMoveFrame,
            "Player.Direction" => Player.Direction,
            "Player.HighMidLowGround" => Player.HighMidLowGround,
            "Player.MoveType" => Player.MoveType,
            "Player.MoveTypeDetailed" => Player.MoveTypeDetailed,
            "Player.Stance" => Player.Stance,
            "Player.StrikeType" => Player.StrikeType,
            "Player.TotalRecovery" => Player.TotalRecovery,
            "Player.TotalStartup" => Player.TotalStartup,
            
            "Opponent.Airborne" => Opponent.Airborne,
            "Opponent.ComboCounter" => Opponent.ComboCounter,
            "Opponent.CurrentCharacter" => Opponent.CurrentCharacter,
            "Opponent.CurrentMove" => Opponent.CurrentMove,
            "Opponent.CurrentMoveFrame" => Opponent.CurrentMoveFrame,
            "Opponent.Direction" => Opponent.Direction,
            "Opponent.HighMidLowGround" => Opponent.HighMidLowGround,
            "Opponent.MoveType" => Opponent.MoveType,
            "Opponent.MoveTypeDetailed" => Opponent.MoveTypeDetailed,
            "Opponent.Stance" => Opponent.Stance,
            "Opponent.StrikeType" => Opponent.StrikeType,
            "Opponent.TotalRecovery" => Opponent.TotalRecovery,
            "Opponent.TotalStartup" => Opponent.TotalStartup,
            _ => throw new InvalidOperationException($"Unknown property: {propertyName}")
        };
    } 
}


public class ConditionNode : Node
{
    public List<ComplexCondition> ComplexConditions { get; set; } = new List<ComplexCondition>();
    public void AddComplexCondition(ComplexCondition complexCondition)
    {
        ComplexConditions.Add(complexCondition);
    }
    public override NodeState Evaluate()
    {
        return ComplexConditions.All(cc => cc.Evaluate()) ? NodeState.Success : NodeState.Failure;
    }

    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("ConditionNode");
        writer.WritePropertyName("ComplexConditions");
        serializer.Serialize(writer, ComplexConditions);
        writer.WriteEndObject();
    }
   
}
public class Condition
{
    public string Property { get; set; }
    public string Operator { get; set; }
    public object Value { get; set; }
}

/*public class OldConditionNode : Node
{
    // [JsonIgnore]
    // public Func<bool> Condition { get; set; }
    //
    // public ConditionNode(Func<bool> condition) => Condition = condition;
    //
    // public override NodeState Evaluate() => Condition() ? NodeState.Success : NodeState.Failure;

    private List<Condition> _conditions;

    public List<Condition> Conditions
    {
        get => _conditions;
        set => _conditions = value;
    }

    public ConditionNode()
    {
        _conditions = new List<Condition>();
    }

    public ConditionNode(List<Condition> conditions)
    {
        _conditions = conditions;
    }

    public void AddCondition(Condition condition)
    {
        _conditions.Add(condition);
    }

    public void AddConditions(List<Condition> conditions)
    {
        _conditions.AddRange(conditions);
    }

    public override NodeState Evaluate()
    {
        if (EvaluateConditions(_conditions))
        {
            return NodeState.Success;
        }
        else
        {
            return NodeState.Failure;
        }
    }


    public override void Serialize(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("NodeType");
        writer.WriteValue("ConditionNode");
        writer.WritePropertyName("Condition");
        // writer.WriteValue(""); // 这里需要序列化实际的条件表达式
        writer.WriteValue(JsonConvert.SerializeObject(Conditions)); // 这里需要序列化实际的条件表达式
        writer.WriteEndObject();
    }

    private object GetPropertyValue(string propertyName)
    {
        Console.WriteLine($"GetPropertyValue: {propertyName}");
        return propertyName switch
        {
            "PX_Distance" => PX_Distance,
            "PX_TotalActiveFrames" => PX_TotalActiveFrames,
            
            "Player.Airborne" => Player.Airborne,
            "Player.ComboCounter" => Player.ComboCounter,
            "Player.CurrentCharacter" => Player.CurrentCharacter,
            "Player.CurrentMove" => Player.CurrentMove,
            "Player.CurrentMoveFrame" => Player.CurrentMoveFrame,
            "Player.Direction" => Player.Direction,
            "Player.HighMidLowGround" => Player.HighMidLowGround,
            "Player.MoveType" => Player.MoveType,
            "Player.MoveTypeDetailed" => Player.MoveTypeDetailed,
            "Player.Stance" => Player.Stance,
            "Player.StrikeType" => Player.StrikeType,
            "Player.TotalRecovery" => Player.TotalRecovery,
            "Player.TotalStartup" => Player.TotalStartup,
            
            "Opponent.Airborne" => Opponent.Airborne,
            "Opponent.ComboCounter" => Opponent.ComboCounter,
            "Opponent.CurrentCharacter" => Opponent.CurrentCharacter,
            "Opponent.CurrentMove" => Opponent.CurrentMove,
            "Opponent.CurrentMoveFrame" => Opponent.CurrentMoveFrame,
            "Opponent.Direction" => Opponent.Direction,
            "Opponent.HighMidLowGround" => Opponent.HighMidLowGround,
            "Opponent.MoveType" => Opponent.MoveType,
            "Opponent.MoveTypeDetailed" => Opponent.MoveTypeDetailed,
            "Opponent.Stance" => Opponent.Stance,
            "Opponent.StrikeType" => Opponent.StrikeType,
            "Opponent.TotalRecovery" => Opponent.TotalRecovery,
            "Opponent.TotalStartup" => Opponent.TotalStartup,
            _ => throw new InvalidOperationException($"Unknown property: {propertyName}")
        };
    }

    private static bool EvaluateCondition(object propertyValue, string operatorSymbol, object conditionValue)
    {
        // 确保两个值都转换为相同的类型（选择范围更大的类型）
        Type commonType = GetCommonType(propertyValue.GetType(), conditionValue.GetType());
        var convertedPropertyValue = Convert.ChangeType(propertyValue, commonType);
        var convertedConditionValue = Convert.ChangeType(conditionValue, commonType);

        if (convertedPropertyValue is IComparable comparablePropertyValue &&
            convertedConditionValue is IComparable comparableConditionValue)
        {
            int comparisonResult = comparablePropertyValue.CompareTo(comparableConditionValue);
            return operatorSymbol switch
            {
                "<=" => comparisonResult <= 0,
                "<" => comparisonResult < 0,
                ">=" => comparisonResult >= 0,
                ">" => comparisonResult > 0,
                "==" => comparisonResult == 0,
                "!=" => comparisonResult != 0,
                _ => throw new InvalidOperationException("Unsupported operator")
            };
        }

        throw new InvalidOperationException("Values are not comparable");
    }

    private static Type GetCommonType(Type type1, Type type2)
    {
        if (type1 == type2) return type1;
        if (type1 == typeof(double) || type2 == typeof(double)) return typeof(double);
        if (type1 == typeof(float) || type2 == typeof(float)) return typeof(float);
        if (type1 == typeof(long) || type2 == typeof(long)) return typeof(long);
        if (type1 == typeof(int) || type2 == typeof(int)) return typeof(int);
        if (type1 == typeof(short) || type2 == typeof(short)) return typeof(short);
        return typeof(int); // 默认使用int
    }

    public bool EvaluateConditions(List<Condition> conditions)
    {
        foreach (var condition in conditions)
        {
            var propertyValue = GetPropertyValue(condition.Property);
            Console.WriteLine($"Property value: {propertyValue} ({propertyValue.GetType()})");
            Console.WriteLine($"Condition value: {condition.Value} ({condition.Value.GetType()})");

            if (!EvaluateCondition(propertyValue, condition.Operator, condition.Value))
            {
                return false;
            }
        }

        return true;
    }

}*/

public class BehaviorTreeSerialized
{
    public static void SaveTree(Node rootNode, string filePath)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto
        };

        string json = JsonConvert.SerializeObject(rootNode, settings);
        File.WriteAllText(filePath, json);
    }

    public static Node LoadTree(string filePath)
    {
        string json = File.ReadAllText(filePath);

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new List<JsonConverter> { new NodeJsonConverter() }
        };

        return JsonConvert.DeserializeObject<Node>(json, settings);
    }
}

public class BTreeExample
{
    /*public static void TestMain(string[] args)
    {
        // 创建行为树
        Node attack = new AttackNode();
        Node defend = new DefendNode();
        Node move = new MoveNode();

        var opThrowConditions = new List<Condition>
        {
            new Condition { Property = "GameInfo.Opponent.MoveTypeDetailed", Operator = "==", TypedValue = new TypedValue {Type = "int", Value = 16} }
        };

        Node conditionOpThrow = new ConditionNode(opThrowConditions);

        var conditions= new List<Condition>
        {
            new Condition { Property = "GameInfo.Opponent.HighMidLowGround", Operator = "<=", TypedValue = new TypedValue{Type = "System.Int32", Value = 2}},
            new Condition { Property = "GameInfo.PX_Distance", Operator = "<", TypedValue = new TypedValue{Type = "System.Float",Value = 2.16 }},
            new Condition { Property = "GameInfo.Opponent.TotalStartup", Operator = "<=", TypedValue = new TypedValue{Type = "System.Int32",Value = 10 }}
            // new Condition { Property = "GameInfo.Opponent.HighMidLowGround", Operator = "<=", Value = 2 },
            // new Condition { Property = "GameInfo.PX_Distance", Operator = "<", Value = 2.16 },
            // new Condition { Property = "GameInfo.Opponent.TotalStartup", Operator = "<=", Value = 10 }
        };

        // string json = JsonConvert.SerializeObject(conditions, Formatting.Indented);
        // File.WriteAllText("conditions.json", json);

        // string json = File.ReadAllText("conditions.json");
        // var conditions = JsonConvert.DeserializeObject<List<Condition>>(json);
        var condsCloseCmdThrow = new List<Condition>
        {
            new Condition{ Property = "GameInfo.Opponent.HighMidLowGround", Operator = "<=", TypedValue = new TypedValue{Type = "System.Int32", Value = 2}},
            new Condition{ Property = "GameInfo.PX_Distance", Operator = "<",TypedValue = new TypedValue{Type = "System.Float",Value = 1.90}},
            new Condition {Property = "GameInfo.Opponent.TotalStartup", Operator = ">=", TypedValue = new TypedValue{Type = "System.Int32",Value =  11}}
        };

         var condsCrouchThrow = new List<Condition>
        {
            new Condition{ Property = "GameInfo.Opponent.HighMidLowGround", Operator = ">=",TypedValue = new TypedValue{Type = "System.Int32", Value = 3}},
            new Condition{ Property = "GameInfo.PX_Distance", Operator = "<",TypedValue = new TypedValue{Type = "System.Float",Value =  2.16}},
        };


        Node selectThrow = new SelectorNode(new List<Node>
        {
            new SequenceNode(new List<Node>
            {
                new ConditionNode(conditions), // 示例条件
                new PerformMoveActionNode("9K")
            }),
            new SequenceNode(new List<Node>
            {
                new ConditionNode(condsCloseCmdThrow), // 示例条件
                new PerformMoveActionNode("2H+K")
            }),
            new SequenceNode(new List<Node>
            {
                new ConditionNode(condsCrouchThrow), // 示例条件
                new PerformMoveActionNode("7K")
            })
        });

        Node counterThrow = new SequenceNode(new List<Node>
        {
            conditionOpThrow,
            selectThrow
        });

        Node root = new SelectorNode(new List<Node>
        {
            counterThrow,
            defend,
            move
        });

        // 保存行为树到文件
        string filePath = "behavior_tree.json";
        BehaviorTreeSerialized.SaveTree(root, filePath);

        // 从文件读取行为树
        Node loadedRoot = BehaviorTreeSerialized.LoadTree(filePath);

        // 评估读取后的行为树
        Console.WriteLine("Starting Behavior Tree Evaluation");
        NodeState result = loadedRoot.Evaluate();
        Console.WriteLine($"Behavior Tree Evaluation Result: {result}");
    }*/
}