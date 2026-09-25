using System;
using R3;
using UnityEngine;
using Zenject;

namespace Yuriy.MatchThree.NetworkMessages.Presentation
{
    public sealed class HelloMessageView : MonoBehaviour
    {
        [Inject] private HelloMessageViewModel _helloMessageViewModel;

        private IDisposable _helloTextSubscription;
        private string _helloText;

        private void Awake()
        {
            _helloTextSubscription = _helloMessageViewModel.HelloText.Subscribe(HandleHelloTextChanged);
        }

        private void OnDestroy()
        {
            _helloTextSubscription.Dispose();
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(20.0f, 20.0f, 160.0f, 40.0f), "Start Host"))
            {
                _helloMessageViewModel.StartHost();
            }

            if (GUI.Button(new Rect(20.0f, 70.0f, 160.0f, 40.0f), "Start Client"))
            {
                _helloMessageViewModel.StartClient();
            }

            GUI.Label(new Rect(20.0f, 130.0f, 400.0f, 40.0f), _helloText);
        }

        private void HandleHelloTextChanged(string helloText)
        {
            _helloText = helloText;
        }
    }
}
