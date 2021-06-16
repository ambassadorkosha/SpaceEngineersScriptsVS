using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Base {
    public class Pair<K, V> {
        public K k;
        public V v;

        public Pair(K k, V v) {
            this.k = k;
            this.v = v;
        }

		public override int GetHashCode()
		{
			return (k?.GetHashCode() ?? 0) + (v?.GetHashCode() ?? 0);
		}

		public override bool Equals(object obj)
		{
			var pair = obj as Pair<K,V>;
			if (pair == null) return false;

			return (((pair.k != null && k != null && pair.k.Equals(k)) || (pair.k == null && k == null)) && ((pair.v != null && v != null && pair.v.Equals(v)) || (pair.v == null && v == null)));

        }
	}

    public class Tripple<K, V, T> {
        public K k;
        public V v;
        public T t;

        public Tripple(K k, V v, T t) {
            this.k = k;
            this.v = v;
            this.t = t;
        }
    }

}
