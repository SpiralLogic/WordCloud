using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCloudTest
{
    class test
    {

        public test()
        {
            var something = "stuff";

            var something2 = Test2(ref something);
            
            Debug.WriteLine(something);
            Debug.WriteLine(something2);

            something2 = "wewe";

            Debug.WriteLine(something);
            Debug.WriteLine(something2);
        }

        private ref string Test2(ref string arg)
        {
            arg = "other stuff";

            return ref arg;
        }
    }
}
