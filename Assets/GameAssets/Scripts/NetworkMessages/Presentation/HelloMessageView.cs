using System;
using System.Collections;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Presentation
{
    public sealed class HelloMessageView : MonoBehaviour
    {
        [SerializeField] private Button _startHostButton;
        [SerializeField] private Button _startClientButton;
        [SerializeField] private Button _stopHostButton;
        [SerializeField] private Button _stopClientButton;
        [SerializeField] private TMP_InputField _networkAddressInput;
        [SerializeField] private TextMeshProUGUI _helloTextLabel;
        [SerializeField] private TextMeshProUGUI _connectionLogLabel;
        [SerializeField] private ScrollRect _helloMessagesScrollRect;
        [SerializeField] private ScrollRect _connectionLogScrollRect;

        [Inject] private HelloMessageViewModel _helloMessageViewModel;

        private IDisposable _helloTextSubscription;
        private IDisposable _networkAddressSubscription;
        private IDisposable _connectionLogSubscription;
        private IDisposable _sessionStateSubscription;
        private IDisposable _hostStartAvailabilitySubscription;

        private void Awake()
        {
            _startHostButton.onClick.AddListener(HandleStartHostClicked);
            _startClientButton.onClick.AddListener(HandleStartClientClicked);
            _stopHostButton.onClick.AddListener(HandleStopHostClicked);
            _stopClientButton.onClick.AddListener(HandleStopClientClicked);
            _networkAddressInput.onEndEdit.AddListener(HandleNetworkAddressInputEnded);
            _helloTextSubscription = _helloMessageViewModel.HelloText.Subscribe(HandleHelloTextChanged);
            _networkAddressSubscription = _helloMessageViewModel.NetworkAddress.Subscribe(HandleNetworkAddressUpdated);
            _connectionLogSubscription = _helloMessageViewModel.ConnectionLog.Subscribe(HandleConnectionLogChanged);
            _sessionStateSubscription = _helloMessageViewModel.SessionState.Subscribe(HandleSessionStateChanged);
            _hostStartAvailabilitySubscription = _helloMessageViewModel.IsHostStartAvailable.Subscribe(HandleHostStartAvailabilityChanged);
        }

        private void OnDestroy()
        {
            _startHostButton.onClick.RemoveListener(HandleStartHostClicked);
            _startClientButton.onClick.RemoveListener(HandleStartClientClicked);
            _stopHostButton.onClick.RemoveListener(HandleStopHostClicked);
            _stopClientButton.onClick.RemoveListener(HandleStopClientClicked);
            _networkAddressInput.onEndEdit.RemoveListener(HandleNetworkAddressInputEnded);
            _helloTextSubscription.Dispose();
            _networkAddressSubscription.Dispose();
            _connectionLogSubscription.Dispose();
            _sessionStateSubscription.Dispose();
            _hostStartAvailabilitySubscription.Dispose();
        }

        private void HandleStartHostClicked()
        {
            _helloMessageViewModel.StartHost();
        }

        private void HandleStartClientClicked()
        {
            _helloMessageViewModel.StartClient();
        }

        private void HandleStopHostClicked()
        {
            _helloMessageViewModel.StopHost();
        }

        private void HandleStopClientClicked()
        {
            _helloMessageViewModel.StopClient();
        }

        private void HandleNetworkAddressInputEnded(string networkAddress)
        {
            _helloMessageViewModel.SetNetworkAddress(networkAddress);
        }

        private void HandleHelloTextChanged(string helloText)
        {
            _helloTextLabel.text = helloText;
            ScrollToBottom(_helloMessagesScrollRect);
        }

        private void HandleNetworkAddressUpdated(string networkAddress)
        {
            _networkAddressInput.SetTextWithoutNotify(networkAddress);
        }

        private void HandleConnectionLogChanged(string connectionLog)
        {
            _connectionLogLabel.text = connectionLog;
            ScrollToBottom(_connectionLogScrollRect);
        }

        private void HandleSessionStateChanged(NetworkSessionStateType sessionState)
        {
            bool isOffline = sessionState == NetworkSessionStateType.Offline;
            bool isHost = sessionState == NetworkSessionStateType.Host;
            bool isClient = sessionState == NetworkSessionStateType.Client;
            _startHostButton.gameObject.SetActive(isOffline || isClient);
            _startHostButton.interactable = isOffline;
            _startClientButton.gameObject.SetActive(isOffline || isHost);
            _startClientButton.interactable = isOffline;
            _stopHostButton.gameObject.SetActive(isHost);
            _stopClientButton.gameObject.SetActive(isClient);
        }

        private void HandleHostStartAvailabilityChanged(bool isHostStartAvailable)
        {
            _startHostButton.interactable = isHostStartAvailable;
        }

        private void ScrollToBottom(ScrollRect scrollRect)
        {
            StartCoroutine(ScrollToBottomAtEndOfFrame(scrollRect));
        }

        private IEnumerator ScrollToBottomAtEndOfFrame(ScrollRect scrollRect)
        {
            yield return new WaitForEndOfFrame();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
