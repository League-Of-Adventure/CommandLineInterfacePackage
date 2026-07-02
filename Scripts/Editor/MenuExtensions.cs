using com.Louis.CommandLineInterface.VContainer;
using Louis.CustomPackages.CommandLineInterface.CommandConfiguration;
using Louis.CustomPackages.CommandLineInterface.Core;
using Louis.CustomPackages.CommandLineInterface.OSC;
using Louis.CustomPackages.CommandLineInterface.UI;
using UnityEditor;
using UnityEngine;
using LogDispatcher = Louis.CustomPackages.CommandLineInterface.Logging.LogDispatcher;
using CommandHandler = Louis.CustomPackages.CommandLineInterface.Core.CommandHandler;

namespace Louis.CustomPackages.CommandLineInterface.Editor {
    public class MenuExtensions : MonoBehaviour {
        [MenuItem("GameObject/Command Line Interface/Set Up All Components", false, 0)]
        private static void CreateAll(MenuCommand menuCommand) {

            // 2. Spawn the backend command processing engine
            CommandHandler handler = CreateCommandManager(menuCommand);

            // 3. Spawn the user interface and decoupling receivers
            Console console = CreateConsole(menuCommand);
            InputCommandMapper mapper = CreateInputCommandMapper(menuCommand);
#if USE_OSC
            OSCCommandReceiver receiver = CreateOSCReceiver(menuCommand);
#endif

#if USE_VCONTAINER
            // 1. Spawn the Core dependency container
            CommandLineLifetimeScope lifetimeScope = CreateLifetimeScope(menuCommand);

            // 4. Connect everything via Unity's Serialized Properties framework
            if(lifetimeScope != null) {
                SerializedObject serializedScope = new SerializedObject(lifetimeScope);

                // --- Part A: Inject Backend Logic Components ---
                if(handler != null) {
                    handler.TryGetComponent<LogDispatcher>(out var logger);
                    handler.TryGetComponent<CommandRegistry>(out var registry);
                    handler.TryGetComponent<CommandCompiler>(out var compiler);

                    serializedScope.FindProperty("_commandHandler").objectReferenceValue = handler;
                    serializedScope.FindProperty("_commandRegistry").objectReferenceValue = registry;
                    serializedScope.FindProperty("_commandLogger").objectReferenceValue = logger;
                    serializedScope.FindProperty("_commandCompiler").objectReferenceValue = compiler;

                    serializedScope.FindProperty("_console").objectReferenceValue = console;
                }

                // --- Part B: Populate VContainer's "Auto-Inject GameObjects" List ---
                SerializedProperty autoInjectProp = serializedScope.FindProperty("autoInjectGameObjects");
                if(autoInjectProp != null && autoInjectProp.isArray) {
                    // Reset the array cleanly just in case the prefab defaulted with anything
                    autoInjectProp.ClearArray();

                    AppendToSerializedArray(autoInjectProp, mapper != null ? mapper.gameObject : null);
#if USE_OSC
                    AppendToSerializedArray(autoInjectProp, receiver != null ? receiver.gameObject : null);
#endif
                }

                // Commit changes permanently to the scene architecture
                serializedScope.ApplyModifiedProperties();
            }

            // 5. Alert the developer about final architectural setups
            Debug.Log("<b>[CommandLine]</b> All components connected. You must provide your own InputProvider instance in the LifetimeScope.");
#else
            Debug.Log("<b>[CommandLine]</b> All components create.");
#endif
        }

#if USE_VCONTAINER
        [MenuItem("GameObject/Command Line Interface/Lifetime Scope", false, 10)]
        private static CommandLineLifetimeScope CreateLifetimeScope(MenuCommand menuCommand) =>
            CreatePrefab<CommandLineLifetimeScope>(menuCommand, "Packages/com.louis.commandlineinterface/Prefabs/LifetimeScope.prefab");
#endif

        [MenuItem("GameObject/Command Line Interface/Command Manager", false, 10)]
        private static CommandHandler CreateCommandManager(MenuCommand menuCommand) =>
            CreatePrefab<CommandHandler>(menuCommand, "Packages/com.louis.commandlineinterface/Prefabs/CommandHandler.prefab");

        [MenuItem("GameObject/Command Line Interface/Console", false, 10)]
        private static Console CreateConsole(MenuCommand menuCommand) =>
            CreatePrefab<Console>(menuCommand, "Packages/com.louis.commandlineinterface/Prefabs/Console.prefab");

        [MenuItem("GameObject/Command Line Interface/Input Command Mapper", false, 10)]
        private static InputCommandMapper CreateInputCommandMapper(MenuCommand menuCommand) =>
            CreatePrefab<InputCommandMapper>(menuCommand, "Packages/com.louis.commandlineinterface/Prefabs/InputCommandMapper.prefab");

#if USE_OSC
        [MenuItem("GameObject/Command Line Interface/OSC Receiver", false, 10)]
        private static OSCCommandReceiver CreateOSCReceiver(MenuCommand menuCommand) =>
            CreatePrefab<OSCCommandReceiver>(menuCommand, "Packages/com.louis.commandlineinterface/Prefabs/OSCReceiver.prefab");
#endif

        static T CreatePrefab<T>(MenuCommand menuCommand, string path) where T : class {
            // 2. Load the asset from the package cache
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if(prefab == null) {
                Debug.LogError($"[CommandLine] Could not find prefab at path: {path}");
                return null;
            }

            // 3. Instantiate it properly as a prefab link (not a loose clone)
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name; // Strip the "(Clone)" suffix

            // 4. Mirror native Unity behavior: Parent it to whatever the user has selected in the Hierarchy
            GameObjectUtility.SetParentAndAlign(instance, menuCommand.context as GameObject);

            // 5. Register the creation with Unity's Undo system so Ctrl+Z works flawlessly
            Undo.RegisterCreatedObjectUndo(instance, $"Create {instance.name}");

            // 6. Automatically select the newly created object
            Selection.activeObject = instance;

            return instance.GetComponent<T>();
        }

        /// <summary>
        /// Helper function to cleanly expand a Serialized Array and assign an element
        /// </summary>
        private static void AppendToSerializedArray(SerializedProperty arrayProp, GameObject target) {
            if(target == null) return;

            int newIndex = arrayProp.arraySize;
            arrayProp.InsertArrayElementAtIndex(newIndex);
            arrayProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = target;
        }
    }
}