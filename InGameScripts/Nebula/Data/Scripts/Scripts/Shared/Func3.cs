using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMod {
    public interface Func3<V, T, K> {
        V run(T t, K k);
    }
}
