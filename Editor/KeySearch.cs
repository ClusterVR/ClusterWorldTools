using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using ClusterVR.CreatorKit.Trigger;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Gimmick.Implements;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.Operation;
using ClusterVR.CreatorKit.Operation.Implements;
using ClusterVR.CreatorKit;
using UnityEngine.UI;
using UnityEngine.Assertions.Must;
using UnityEditor.SceneManagement;

namespace ClusterWorldTools
{
    public class KeySearch : EditorWindow
    {
        static bool hierarchyChangedFlag = false;

        string nameQuery = string.Empty;
        enum Target
        {
            None,
            Item,
            Player,
            Global
        }
        Target targetQuery = Target.None;
        [SerializeField]GameObject itemQuery = null;
        GameObject itemQuery_before = null;

        Vector2 triggersScrollPosition, gimmicksScrollPosition, logicsScrollPosition;

        bool showGameObjectName, showComponentName;

        struct FoundTrigger
        {
            public TriggerParam triggerParam { get; private set; }
            public GameObject gameObject { get; private set; }
            public ITrigger triggerComponent { get; private set; }

            public FoundTrigger(TriggerParam _triggerParam, GameObject _gameObject, ITrigger _triggerComponent)
            {
                triggerParam = _triggerParam;
                gameObject = _gameObject;
                triggerComponent = _triggerComponent;
            }
        }

        struct FoundGimmick
        {
            public IGimmick gimmick { get; private set; }
            public GameObject gameObject { get; private set; }
            public FoundGimmick(IGimmick _gimmick, GameObject _gameObject)
            {
                gimmick = _gimmick;
                gameObject = _gameObject;
            }
        }

        struct FoundLogic
        {
            public Expression expression { get; private set; }
            public ILogic logicComponent { get; private set; }
            public GameObject gameObject { get; private set; }
            public FoundLogic(Expression _expression, GameObject _gameObject, ILogic _logicComponent)
            {
                expression = _expression;
                gameObject = _gameObject;
                logicComponent = _logicComponent;
            }
        }

        List<FoundTrigger> foundTriggers = new List<FoundTrigger>();
        List<FoundGimmick> foundGimmicks = new List<FoundGimmick>();
        List<FoundLogic> foundLogics = new List<FoundLogic>();

        [MenuItem("WorldTools/Key検索")]
        static public void CreateWindow()
        {
            var window = GetWindow<KeySearch>();
            window.titleContent = new GUIContent("Key検索");
            window.FindKey();
        }

        [InitializeOnLoadMethod]
        public static void SetCallBack()
        {
            EditorApplication.hierarchyChanged += HierarchyChanged;
        }

        public static void HierarchyChanged()
        {
            hierarchyChangedFlag = true;
        }

        public void OnGUI()
        {
            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(64)))
            {
                nameQuery = string.Empty;
                targetQuery = Target.None;
                itemQuery = null;
            }

            bool search = hierarchyChangedFlag;
            hierarchyChangedFlag = false;

            var _nameQuery = EditorGUILayout.TextField(new GUIContent("Key"), nameQuery);
            if(nameQuery != _nameQuery)
            {
                search = true;
                nameQuery = _nameQuery;
            }
            var _targetQuery = (Target)EditorGUILayout.EnumPopup("Target", targetQuery);
            if(targetQuery != _targetQuery)
            {
                search = true;
                targetQuery = _targetQuery;
            }

            using (new EditorGUI.DisabledScope(targetQuery != Target.Item))
            {
                var serialized = new SerializedObject(this);
                serialized.Update();
                var itemQueryProperty = serialized.FindProperty(nameof(itemQuery));
                EditorGUILayout.PropertyField(itemQueryProperty, new GUIContent("Item"));
                if(itemQuery_before != itemQuery)
                {
                    search = true;
                    itemQuery_before = itemQuery;
                }
                serialized.ApplyModifiedProperties();
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"))) search = true;

            if (search) FindKey();

            showGameObjectName = EditorGUILayout.ToggleLeft("オブジェクト名を表示", showGameObjectName);
            showComponentName = EditorGUILayout.ToggleLeft("コンポーネント名を表示", showComponentName);

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                var labelStyle = new GUIStyle(GUIStyle.none);
                labelStyle.fixedWidth = this.position.width / 3;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.normal.textColor = Color.white;

                var buttonStyle = new GUIStyle(EditorStyles.miniButton);
                buttonStyle.alignment = TextAnchor.MiddleLeft;
                //buttonStyle.stretchHeight = true;
                buttonStyle.fixedHeight = 0f;

                using (var scope = new EditorGUILayout.VerticalScope(GUILayout.Width(labelStyle.fixedWidth)))
                {
                    EditorGUILayout.LabelField("Triggers", labelStyle);
                    using (var scroll = new EditorGUILayout.ScrollViewScope(triggersScrollPosition))
                    {
                        foreach (var trigger in foundTriggers)
                        {
                            if(trigger.gameObject == null) continue;
                            if (GUILayout.Button(ButtonLabel(trigger.triggerParam.RawKey, trigger.gameObject.name, trigger.triggerComponent.GetType().Name), buttonStyle))
                            {
                                Selection.activeGameObject = trigger.gameObject;
                                nameQuery = trigger.triggerParam.RawKey;
                                itemQuery = null;
                                switch (trigger.triggerParam.Target)
                                {
                                    case TriggerTarget.Item:
                                        targetQuery = Target.Item;
                                        itemQuery = trigger.gameObject;
                                        break;
                                    case TriggerTarget.SpecifiedItem:
                                        targetQuery = Target.Item;
                                        itemQuery = trigger.triggerParam.SpecifiedTargetItem.gameObject;
                                        break;
                                    case TriggerTarget.Player:
                                        targetQuery = Target.Player;
                                        break;
                                    case TriggerTarget.CollidedItemOrPlayer:
                                        targetQuery = Target.None;
                                        break;
                                    case TriggerTarget.Global:
                                        targetQuery = Target.Global;
                                        break;
                                }
                            }
                        }
                        triggersScrollPosition = scroll.scrollPosition;
                    }
                }
                using (var scope = new EditorGUILayout.VerticalScope(GUILayout.Width(labelStyle.fixedWidth)))
                {
                    EditorGUILayout.LabelField("Gimmicks", labelStyle);
                    using (var scroll = new EditorGUILayout.ScrollViewScope(gimmicksScrollPosition))
                    {
                        foreach (var gimmick in foundGimmicks)
                        {
                            if(gimmick.gameObject == null) continue;
                            if (GUILayout.Button(ButtonLabel(gimmick.gimmick.Key, gimmick.gameObject.name, gimmick.gimmick.GetType().Name), buttonStyle))
                            {
                                Selection.activeGameObject = gimmick.gameObject;
                                nameQuery = gimmick.gimmick.Key;
                                itemQuery = null;
                                switch(gimmick.gimmick.Target)
                                {
                                    case GimmickTarget.Item:
                                        targetQuery = Target.Item;
                                        var allItems = FindObjectsOfType<Item>();
                                        foreach (var item in allItems)
                                        {
                                            if (item.Id.Value == 0L) item.Id = ItemId.Create();
                                            if(gimmick.gimmick.ItemId.Value == item.Id.Value)
                                            {
                                                itemQuery = item.gameObject;
                                                break;
                                            }
                                        }
                                        break;
                                    case GimmickTarget.Player:
                                        targetQuery = Target.Player;
                                        break;
                                    case GimmickTarget.Global:
                                        targetQuery = Target.Global;
                                        break;
                                }
                            }
                        }
                        gimmicksScrollPosition = scroll.scrollPosition;
                    }
                }
                using (var scope = new EditorGUILayout.VerticalScope(GUILayout.Width(labelStyle.fixedWidth)))
                {
                    EditorGUILayout.LabelField("Logics", labelStyle);
                    using (var scroll = new EditorGUILayout.ScrollViewScope(logicsScrollPosition))
                    {
                        foreach (var logic in foundLogics)
                        {
                            if(logic.gameObject == null) continue;
                            if (GUILayout.Button(ButtonLabel(logic.expression.Value.SourceState.Key, logic.gameObject.name, logic.logicComponent.GetType().Name), buttonStyle))
                            {
                                Selection.activeGameObject = logic.gameObject;
                                nameQuery = logic.expression.Value.SourceState.Key;
                                itemQuery = null;
                                switch (logic.expression.Value.SourceState.Target)
                                {
                                    case GimmickTarget.Item:
                                        targetQuery = Target.Item;
                                        itemQuery = logic.gameObject;
                                        break;
                                    case GimmickTarget.Player:
                                        targetQuery = Target.Player;
                                        break;
                                    case GimmickTarget.Global:
                                        targetQuery = Target.Global;
                                        break;
                                }
                            }
                        }
                        logicsScrollPosition = scroll.scrollPosition;
                    }
                }
            }
        }

        string ButtonLabel(string key, string gameObject, string component)
        {
            string label = $"Key: {key}";
            if (showComponentName) label = $"Component: {component}\n{label}";
            if (showGameObjectName) label = $"GameObject: {gameObject}\n{label}";
            return label;
        }

        void FindKey()
        {
            var allGameObjects = FindObjectsOfType<GameObject>();
            var triggers = allGameObjects.Where(trigger => trigger.GetComponent<ITrigger>() != null);
            var gimmicks = allGameObjects.Where(gimmick => gimmick.GetComponent<IGimmick>() != null);
            var logics = allGameObjects.Where(logic => logic.GetComponent<ILogic>() != null);

            foundTriggers.Clear();
            foreach (var trigger in triggers)
            {
                foreach (var triggerComponent in trigger.GetComponents<ITrigger>())
                {
                    if (triggerComponent.TriggerParams == null) continue;
                    foreach (var param in triggerComponent.TriggerParams)
                    {
                        if (nameQuery != string.Empty && nameQuery != param.RawKey) continue;
                        switch (targetQuery)
                        {
                            case Target.Item:
                                if (param.Target != TriggerTarget.Item && param.Target != TriggerTarget.SpecifiedItem && param.Target != TriggerTarget.CollidedItemOrPlayer) continue;
                                if (param.Target == TriggerTarget.Item && itemQuery != null && itemQuery != trigger) continue;
                                if (param.Target == TriggerTarget.SpecifiedItem && itemQuery != null && (param.SpecifiedTargetItem == null || itemQuery != param.SpecifiedTargetItem.gameObject)) continue;
                                break;
                            case Target.Player:
                                if (param.Target != TriggerTarget.Player && param.Target != TriggerTarget.CollidedItemOrPlayer) continue;
                                break;
                            case Target.Global:
                                if (param.Target != TriggerTarget.Global) continue;
                                break;
                        }

                        foundTriggers.Add(new FoundTrigger(param, trigger, triggerComponent));
                    }
                }
            }
            foundGimmicks.Clear();
            foreach (var gimmick in gimmicks)
            {
                foreach (var gimmickComponent in gimmick.GetComponents<IGimmick>())
                {
                    if (nameQuery != string.Empty && nameQuery != gimmickComponent.Key) continue;
                    switch (targetQuery)
                    {
                        case Target.Item:
                            if (gimmickComponent.Target != GimmickTarget.Item) continue;
                            if (itemQuery != null)
                            {
                                var itemQueryComponent = itemQuery.GetComponent<Item>();
                                if (itemQueryComponent == null) continue;

                                // Item IDで識別するため、ID初期化されていない場合は初期化する
                                if (itemQueryComponent.Id.IsValid() == false) itemQueryComponent.Id = ItemId.Create();
                                if (itemQueryComponent.Id.Value != gimmickComponent.ItemId.Value) continue;
                            }
                            break;
                        case Target.Player:
                            if (gimmickComponent.Target != GimmickTarget.Player) continue;
                            break;
                        case Target.Global:
                            if (gimmickComponent.Target != GimmickTarget.Global) continue;
                            break;
                    }

                    foundGimmicks.Add(new FoundGimmick(gimmickComponent, gimmick));
                }
            }

            foundLogics.Clear();
            foreach (var logic in logics)
            {
                foreach (var logicComponent in logic.GetComponents<ILogic>())
                {
                    foreach (var statement in logicComponent.Logic.Statements)
                    {
                        int operandNum = statement.SingleStatement.Expression.OperatorExpression.Operator.GetRequiredLength();
                        if (statement.SingleStatement.Expression.Type == ExpressionType.Value) operandNum = 1;
                        for (int i = 0; i < operandNum; i++)
                        {
                            Expression operand;
                            if (statement.SingleStatement.Expression.Type == ExpressionType.Value) operand = statement.SingleStatement.Expression;
                            else operand = statement.SingleStatement.Expression.OperatorExpression.Operands[i];

                            if (operand == null) continue;

                            if (operand.Value.Type == ValueType.Constant) continue;

                            if (nameQuery != string.Empty && nameQuery != operand.Value.SourceState.Key) continue;
                            switch (targetQuery)
                            {
                                case Target.Item:
                                    if (operand.Value.SourceState.Target != GimmickTarget.Item) continue;
                                    if (itemQuery != null && (logicComponent is ItemLogic) && itemQuery != logic) continue;
                                    break;
                                case Target.Player:
                                    if (operand.Value.SourceState.Target != GimmickTarget.Player) continue;
                                    break;
                                case Target.Global:
                                    if (operand.Value.SourceState.Target != GimmickTarget.Global) continue;
                                    break;
                            }

                            foundLogics.Add(new FoundLogic(operand, logic, logicComponent));
                        }
                    }
                }
            }
        }
    }
}