using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.UI.Embedillo
{
    public class ControlPositionManager
    {
        private readonly List<ControlPositionInfo> _positions = new ();

        public IReadOnlyList<ControlPositionInfo> Positions => _positions;

        public void AddControl(int offset, int length)
        {
            RemoveControlWithOffset(offset);
            _positions.Add(new (offset, length));

            //MainWindow.ControlCountLabelStatic.Content = $"control count: {_positions.Count}";
        }

        public void ReplaceControl(
            ControlPositionInfo offsetInfoToRemove,
            int offset,
            int length
            )
        {
            _positions.RemoveAll(i => ReferenceEquals(offsetInfoToRemove, i));
            _positions.Add(new(offset, length));

            //MainWindow.ControlCountLabelStatic.Content = $"control count: {_positions.Count}";
        }

        public void RemoveControlWithOffset(int offset)
        {
            _positions.RemoveAll(i => i.Offset == offset);

            //MainWindow.ControlCountLabelStatic.Content = $"control count: {_positions.Count}";
        }

        public bool IsNearControl(int caretOffset)
        {
            // Проверяем, находится ли курсор рядом с контролом
            return TryGetNearControl(caretOffset) != null;
        }

        public ControlPositionInfo? TryGetNearControl(int caretOffset)
        {
            // Проверяем, находится ли курсор рядом с контролом
            // и возвращаем контрол, если нашли
           var result = TryGetNearControlLeft(caretOffset);
            result ??= TryGetNearControlRight(caretOffset);

            return result;
        }

        private ControlPositionInfo? TryGetNearControlLeft(int caretOffset)
        {
            // Проверяем, находится ли курсор правее контрола
            // и возвращаем контрол, если нашли
            return _positions.FirstOrDefault(i =>
                caretOffset == i.Offset + i.Length
                || caretOffset == i.Offset + i.Length - 1
                );
        }
        private ControlPositionInfo? TryGetNearControlRight(int caretOffset)
        {
            // Проверяем, находится ли курсор левее контрола
            // и возвращаем контрол, если нашли
            return _positions.FirstOrDefault(i =>
                caretOffset == i.Offset
                );
        }
    }
}