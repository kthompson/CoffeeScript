using System;

namespace CoffeeScript
{
	public static class NodeUnwrapper
	{
		public static Base Unwrap(Base node)
		{
			Helper.Break();
			return node;
		}

        public static Base Unsoak(Base node)
        {
            Helper.Break();
            return node;
        }
	}
}

