using System;
using System.Collections.Generic;

namespace Scripts.Shared {
    public class ObjectPool<T> {
        int max = 10;
        List<T> data = new List<T>(10);
        Func <T> creator;
        Action <T> cleaner;

        public ObjectPool (Func <T> creator, Action <T> cleaner) {
            this.cleaner = cleaner;
            this.creator = creator;
        }

        public T get () {
            if (data.Count > 0) {
                return data.Pop();
            } else {
                return creator.Invoke();
            }
        }

        public void put (T item) {
            if (data.Count < max) {
                data.Add (item);
            } else {
                if (cleaner != null) cleaner.Invoke(item);
            }
        }
    }
}
