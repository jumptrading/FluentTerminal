using FluentTerminal.RuntimeComponent.Enums;
using FluentTerminal.RuntimeComponent.Interfaces;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace FluentTerminal.RuntimeComponent.WebAllowedObjects
{
    [AllowForWeb]
    public sealed class TerminalBridge
    {
        private IxtermEventListener _terminalEventListener;

        public TerminalBridge(IxtermEventListener terminalEventListener)
        {
            _terminalEventListener = terminalEventListener;
            _terminalEventListener.OnOutput += _terminalEventListener_OnOutput;
        }

        private void _terminalEventListener_OnOutput(object sender, object e)
        {
            Task.Factory.StartNew(() => Output?.Invoke(this, e));
        }

        public event EventHandler<object> Output;

        public void InputReceived(string message)
        {
            _terminalEventListener?.OnInput(message);
        }

        // This method won't be called because it's currently commented out in index.ts. But I suggest leaving it here in case that we decide to use it.
        public void KeyReceived(string key, string code, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey, string locale, int location, bool repeat)
        {
            System.Diagnostics.Trace.Write($"{nameof(key)}='{key}'; {nameof(code)}='{code}'; {nameof(ctrlKey)}={ctrlKey}; {nameof(shiftKey)}={shiftKey}; {nameof(altKey)}={altKey}; {nameof(metaKey)}={metaKey}; {nameof(locale)}='{locale}'; {nameof(location)}={location}; {nameof(repeat)}={repeat}");
        }

        public void Initialized()
        {
            _terminalEventListener.OnInitialized();
        }

        public void DisposalPrepare()
        {
            _terminalEventListener.OnOutput -= _terminalEventListener_OnOutput;
            _terminalEventListener = null;
        }

        public void NotifySizeChanged(int columns, int rows)
        {
            _terminalEventListener?.OnTerminalResized(columns, rows);
        }

        public void NotifyTitleChanged(string title)
        {
            _terminalEventListener?.OnTitleChanged(title);
        }

        public void InvokeCommand(string command)
        {
            _terminalEventListener?.OnKeyboardCommand(command);
        }

        public void NotifyRightClick(int x, int y, bool hasSelection)
        {
            _terminalEventListener?.OnMouseClick(MouseButton.Right, x, y, hasSelection);
        }

        public void NotifyMiddleClick(int x, int y, bool hasSelection)
        {
            _terminalEventListener?.OnMouseClick(MouseButton.Middle, x, y, hasSelection);
        }

        public void NotifySelectionChanged(string selection)
        {
            _terminalEventListener?.OnSelectionChanged(selection);
        }

        public void ReportError(string error)
        {
            _terminalEventListener?.OnError(error);
        }
    }
}