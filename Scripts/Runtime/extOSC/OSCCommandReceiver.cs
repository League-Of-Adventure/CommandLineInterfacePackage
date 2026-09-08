using Louis.CustomPackages.CommandLineInterface.Core;
using UnityEngine;

using VContainer;

#if USE_EXTOSC
using extOSC;
#endif

namespace Louis.CustomPackages.CommandLineInterface.OSC {
#if USE_EXTOSC
    public class OSCCommandReceiver : MonoBehaviour {
        [Header("Settings")]
        [SerializeField] string _address = "/command";
        [SerializeField] int _localPort = 7001;
        [Inject] readonly ICommandHandler _commandHandler;

        OSCReceiver _receiver;
        OSCBind _binding;

        private void Awake() {
            _receiver = gameObject.AddComponent<OSCReceiver>();
            _receiver.LocalPort = _localPort;
            _binding = new OSCBind(_address, CommandReceived);
        }

        private void OnEnable() {
            _receiver.Bind(_binding);
        }

        private void OnDisable() {
            _receiver.Unbind(_binding);
        }

        public void CommandReceived(OSCMessage message) {
            if(message.Values.Count == 0) return;
            string cmdName = message.Values[0].StringValue;

            int argCount = message.Values.Count - 1;
            string[] parameters = new string[argCount];
            for(int i = 0; i < argCount; i++) {
                parameters[i] = message.Values[i + 1].StringValue;
            }
            _commandHandler?.PushCommand(cmdName, parameters);
        }
    }
#endif
}