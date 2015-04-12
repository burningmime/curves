using System;
using System.Globalization;
using System.Text;
using System.Windows.Input;

namespace burningmime.util.wpf
{
    /// <summary>
    /// A KeyGesture that supports pressing multiple keys in sequence, like Visual Studio's "Ctrl+W, O" to show output window.
    /// </summary>
    public sealed class MultiKeyGesture : KeyGesture
    {
        private static readonly TimeSpan KEY_PRESS_DELAY = TimeSpan.FromSeconds(1);
        private static readonly KeyConverter _keyConverter = new KeyConverter();
        private readonly Key[] _keys;
        private int _index;
        private DateTime _lastKeyTime;

        public MultiKeyGesture(ModifierKeys modifiers, params Key[] keys) : base(Key.None, modifiers, getDisplayString(modifiers, keys)) { _keys = keys; }
        private static string getDisplayString(ModifierKeys modifiers, Key[] keys)
        {
            if(keys == null) throw new ArgumentNullException("keys");
            if(keys.Length < 1) throw new ArgumentException("keys must have at least one member");
            StringBuilder ret = new StringBuilder();
            ret.Append(new KeyGesture(keys[0], modifiers).GetDisplayStringForCulture(CultureInfo.InvariantCulture));
            for(int i = 1; i < keys.Length; i++)
            {
                ret.Append(", ");
                ret.Append(_keyConverter.ConvertTo(null, CultureInfo.InvariantCulture, keys[i], typeof(string)));
            }
            return ret.ToString();
        }

        public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
        {
            // Check for valid key
            KeyEventArgs args = inputEventArgs as KeyEventArgs;
            if(args == null || args.Key <= Key.None || args.Key >= Key.OemClear) return false;

            if(_index != 0 && DateTime.Now - _lastKeyTime > KEY_PRESS_DELAY) _index = 0;  // They were too slow
            if(_index == 0 && Modifiers != Keyboard.Modifiers)               goto Lreset; // On first key press, check modifier keys
            if(_keys[_index] != args.Key)                                    goto Lreset; // Check current key against index
            
            // If they reached the end of the index, return true
            if(++_index == _keys.Length) { _index = 0; return true; }

            // Otherwise record current time and return false
            _lastKeyTime = DateTime.Now;
            inputEventArgs.Handled = true;
            return false;

        Lreset:
            _index = 0;
            return false;
        }
    }
}