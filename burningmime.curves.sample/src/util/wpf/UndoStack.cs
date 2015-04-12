using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace burningmime.util.wpf
{
    public sealed class UndoStack
    {
        public readonly int maxDepth;
        private readonly LinkedList<IMemento> _undo = new LinkedList<IMemento>();
        private readonly LinkedList<IMemento> _redo = new LinkedList<IMemento>();
        private int _currentlyExecuting;
        private int _savePos;
        private int _pos;

        public UndoStack(int maxDepth)
        {
            if(maxDepth < 1) throw new ArgumentOutOfRangeException("maxDepth", "maxDepth must be at least 1");
            this.maxDepth = maxDepth;
        }

        public void push(IMemento memento)
        {
            if(_currentlyExecuting != 0)
                throw new InvalidOperationException("Items cannot be pushed onto the undo/redo stack while undoing/redoing");
            Debug.Assert(null != memento);
            _redo.Clear();
            push(_undo, memento);
            _pos++;
        }

        public bool undo() 
        {
            if (_undo.Count == 0)
                return false;
            _currentlyExecuting++;
            IMemento memento = pop(_undo);
            memento.undo();
            push(_redo, memento);
            _currentlyExecuting--;
            _pos--;
            return true;
        }

        public bool redo() 
        {
            if (_redo.Count == 0)
                return false;
            _currentlyExecuting++;
            IMemento memento = pop(_redo);
            memento.redo();
            push(_undo, memento);
            _currentlyExecuting--;
            _pos++;
            return true;
        }

        public bool canUndo() { return _undo.Count > 0; }
        public bool canRedo() { return _redo.Count > 0; }

        private void push(LinkedList<IMemento> stack, IMemento item)
        {
            Debug.Assert(null != item);
            if(stack.Count > maxDepth)
                stack.RemoveLast();
            stack.AddFirst(item);
        }

        private static IMemento pop(LinkedList<IMemento> stack)
        {
            Debug.Assert(stack.Count > 0);
            IMemento item = stack.First.Value;
            stack.RemoveFirst();
            return item;
        }

        public bool hasUnsavedChanges { get { return _savePos != _pos; } }
        public void markSave() { _savePos = _pos; }

        public void clear()
        {
            if(_currentlyExecuting != 0)
                throw new InvalidOperationException("Cannot clear undo stack during undo/redo operation");
            _undo.Clear();
            _redo.Clear();
            _pos = 0;
        }
    }

    public interface IMemento
    {
        string name { get; }
        void undo();
        void redo();
    }

    public sealed class Memento : IMemento
    {
        public string name { get; set; }
        public override string ToString() { return name; }

        public readonly Action revert;
        public readonly Action exec;

        public Memento(Action exec, Action revert, string name = null)
        {
            if(revert == null) throw new ArgumentNullException("revert");
            if(exec == null) throw new ArgumentNullException("exec");
            this.revert = revert;
            this.exec = exec;
            this.name = name;
        }

        public void undo() { revert(); }
        public void redo() { exec(); }
    }

    public static class PropertyMemento { public static PropertyMemento<TObject, TProperty> get<TObject, TProperty>
        (Action<TObject, TProperty> set, TObject obj, TProperty oldValue, TProperty newValue, string name = null) {
        return new PropertyMemento<TObject, TProperty>(set, obj, oldValue, newValue, name); } }
    public sealed class PropertyMemento<TObject, TProperty> : IMemento
    {
        public string name { get; set; }
        public override string ToString() { return name; }

        public readonly Action<TObject, TProperty> set;
        public readonly TObject obj;
        public readonly TProperty oldValue;
        public readonly TProperty newValue;

        public PropertyMemento(Action<TObject, TProperty> set, TObject obj, TProperty oldValue, TProperty newValue, string name)
        {
            this.set = set;
            this.obj = obj;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.name = name;
        }

        public void undo() { set(obj, oldValue); }
        public void redo() { set(obj, newValue); }
    }

    public sealed class MultiMemento : IMemento
    {
        public string name { get; set; }
        public override string ToString() { return name; }

        private readonly LinkedList<Action> _undo = new LinkedList<Action>();
        private readonly LinkedList<Action> _redo = new LinkedList<Action>();

        public MultiMemento(string name)
        {
            this.name = name;
        }

        public void undo()
        {
            foreach(Action action in _undo)
                action();
        }

        public void redo()
        {
            foreach (Action action in _redo)
                action();
        }

        public void add(IMemento a) 
        { 
            if(null != a)
            {
                Memento u = a as Memento;
                if(null != u)
                {
                    addUndo(u.revert);
                    addRedo(u.exec);
                }
                else
                {
                    addUndo(a.undo);
                    addRedo(a.redo);
                }
            } 
        }

        public void addUndo(Action a) { if(null != a) _undo.AddFirst(a); }
        public void addRedo(Action a) { if(null != a) _redo.AddLast(a); }
    }
}