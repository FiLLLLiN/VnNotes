using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Разбирает параметры командной строки.
    /// </summary>
    public sealed class CommandReader
    {
        private readonly string[] _args;

        public CommandReader(string[] args)
        {
            _args = args;
        }

        public bool Has(string option)
        {
            return _args.Any(x => string.Equals(x, option, StringComparison.OrdinalIgnoreCase));
        }

        public bool TryGetValue(string option, out string value)
        {
            value = "";

            int index = Array.FindIndex(
                _args,
                x => string.Equals(x, option, StringComparison.OrdinalIgnoreCase));

            if (index < 0 || index + 1 >= _args.Length)
                return false;

            value = _args[index + 1];
            return true;
        }

        public string[] GetValues(string option, int count)
        {
            int index = Array.FindIndex(
                _args,
                x => string.Equals(x, option, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
                return null;

            if (index + count >= _args.Length)
                return null;

            return _args
                .Skip(index + 1)
                .Take(count)
                .ToArray();
        }
    }
}
