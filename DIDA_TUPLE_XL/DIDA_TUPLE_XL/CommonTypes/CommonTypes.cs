using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using System.Runtime.Remoting;

namespace RemotingSample
{
    public class MyRemoteObject : MarshalByRefObject, MyRemoteInterface
    {

        private IDictionary<string, List<List<Field>>> tupleSpace = new Dictionary<string, List<List<Field>>>();
        bool crash = false;
        bool freeze = false;
        private int max_delay;
        private int min_delay;

        public MyRemoteObject() { }

        public MyRemoteObject(int min, int max) {
            min_delay = min;
            max_delay = max;
        }

        public void setCrash(bool c)
        {
            crash = c;
        }
        public void setFreeze(bool c)
        {
            freeze = c;
        }

        public string MetodoOla()
        {
            return "ola!";
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int add(List<string> l)
        {
            if (crash)
                throw new RemotingException();
            while (freeze)
                continue;

            Random r = new Random();
            int rInt = r.Next(min_delay, max_delay); //for ints
            System.Threading.Thread.Sleep(rInt);

            List<Field> aux = new List<Field>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            string key;
            if (l == null)
                return 1;
            string first = l[0];
            if (!first.Contains("("))
                key = first.Replace("\"", "");
            else
            {
                string[] key2 = first.Split(new char[] { '(', ')', ',' });
                key = key2[0];

            }
            if (first.Contains("*") || first.Equals("null"))
                return 1;


            foreach (string s in l)
            {     
                if (!s.Contains("("))
                {
                    if (s.Contains("\"") || s.Equals("null"))
                        aux.Add(new Field(s.Replace("\"","")));
                    else
                    {
                        Type type = assembly.GetType("RemotingSample." + s);
                        aux.Add(new Field(Activator.CreateInstance(type), 2));
                    }
                }
                else
                {
                    string[] splited = s.Split(new char[] { '(', ')', ',' });
                    string func = splited[0];
                    object[] o = new object[splited.Length - 2];
                    for (int i = 1; i < splited.Length - 1; i++)
                        o[i - 1] = funcArgs(splited[i]);
                    Type type = assembly.GetType("RemotingSample." + func);
                    aux.Add(new Field(Activator.CreateInstance(type, o), 0));


                }
                
            }

            if (tupleSpace.ContainsKey(key))
                tupleSpace[key].Add(aux);
            
            else
            {           
                List<List<Field>> aux2 = new List<List<Field>>();
                aux2.Add(aux);
                tupleSpace.Add(key, aux2);
                
            }

            return 1;

        }


        public List<List<Field>> readTuple(List<string> l)
        {
            if (crash)
                throw new RemotingException();

            while (freeze)
                continue;

            Random r = new Random();
            int rInt = r.Next(min_delay, max_delay); //for ints
            System.Threading.Thread.Sleep(rInt);

            List<Field> aux = new List<Field>();
            List<List<Field>> aux2 = new List<List<Field>>();
            Assembly assembly = Assembly.GetExecutingAssembly();

            if (l == null)
            {
                return null;
            }
            string first = l[0];
            int founded = 0;
            string key;

            if (!first.Contains("("))
                key = first.Replace("\"", "");
            else
            {
                string[] key2 = first.Split(new char[] { '(', ')', ',' });
                key = key2[0];
            }


            foreach (string s in l)
            {
                if (!s.Contains("("))
                {
                    if (s.Contains("\"") || s.Equals("null"))
                        aux.Add(new Field(s.Replace("\"","")));
                    else
                    {
                        Type type = assembly.GetType("RemotingSample." + s);
                        aux.Add(new Field(Activator.CreateInstance(type), 2));
                    }
                }
                else
                {
                    string[] splited = s.Split(new char[] { '(', ')', ',' });
                    string func = splited[0];
                    object[] o = new object[splited.Length - 2];
                    for (int i = 1; i < splited.Length - 1; i++)
                        o[i - 1] = funcArgs(splited[i]);
                    Type type = assembly.GetType("RemotingSample." + func);
                    aux.Add(new Field(Activator.CreateInstance(type, o), 0));


                }

            }


            List<string> keys = new List<string>();
            if (key.Equals("null") || key.Equals("*"))
            {
                keys = tupleSpace.Keys.ToList();
            }
            else if (key.Contains("*"))
            {
                string[] subKey = key.Split('*');
                if (key.StartsWith("*"))
                {
                    foreach(string k in tupleSpace.Keys)
                    {
                        if (k.EndsWith(subKey[1]))
                            key = k;
                    }
                    keys.Add(key);
                }
                else if (key.EndsWith("*"))
                {
                    foreach (string k in tupleSpace.Keys)
                    {
                        if (k.StartsWith(subKey[0]))
                            key = k;
                    }
                    keys.Add(key);
                }
            }
            else keys.Add(key);

            foreach (string k in keys) {
                foreach (List<Field> ls in tupleSpace[k])
                {
                    if (!(ls.Count == l.Count))
                        continue;
                    bool eq = true;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (aux[i].equals(ls[i]))
                        {
                            continue;
                        }
                        else
                        {
                            eq = false;
                            break;
                        }

                    }
                    if (eq)
                    {
                        aux2.Add(ls);
                        founded = 1;
                    }
                }
            }
            if (founded == 1)
                return aux2;
            else
                return null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<List<Field>> take(List<string> l)
        {

            if (crash)
                throw new RemotingException();

            while (freeze)
                continue;
            Random r = new Random();
            int rInt = r.Next(min_delay, max_delay); //for ints
            System.Threading.Thread.Sleep(rInt);

            List<Field> aux = new List<Field>();
            List<List<Field>> aux2 = new List<List<Field>>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            List<int> takes = new List<int>();

            if (l == null)
                return null;
            string first = l[0];
            int founded = 0;
            string key;
            
            if (!first.Contains("("))
                key = first.Replace("\"", "");
            else
            {
                string[] key2 = first.Split(new char[] { '(', ')', ',' });
                key = key2[0];
                
            }


            foreach (string s in l)
            {
                if (!s.Contains("("))
                {
                    if (s.Contains("\"") || s.Equals("null"))
                        aux.Add(new Field(s.Replace("\"", "")));
                    else
                    {
                        Type type = assembly.GetType("RemotingSample." + s);              
                        aux.Add(new Field(Activator.CreateInstance(type), 2));
                    }
                }
                else
                {
                    string[] splited = s.Split(new char[] { '(', ')', ',' });
                    string func = splited[0];
                    object[] o = new object[splited.Length - 2];
                    for (int i = 1; i < splited.Length-1; i++)
                        o[i - 1] = funcArgs(splited[i]);
                    Type type = assembly.GetType("RemotingSample." + func);
                    aux.Add(new Field(Activator.CreateInstance(type, o), 0));


                }

            }

            List<string> keys = new List<string>();
            if (key.Equals("null") || key.Equals("*"))
            {
                keys = tupleSpace.Keys.ToList();
            }
            else if (key.Contains("*"))
            {
                string[] subKey = key.Split('*');
                if (key.StartsWith("*"))
                {
                    foreach (string k in tupleSpace.Keys)
                    {
                        if (k.EndsWith(subKey[1]))
                            key = k;
                    }
                    keys.Add(key);
                }
                else if (key.EndsWith("*"))
                {
                    foreach (string k in tupleSpace.Keys)
                    {
                        if (k.StartsWith(subKey[1]))
                            key = k;
                    }
                    keys.Add(key);
                }
            }
            else keys.Add(key);
            
            foreach (string k in keys)
            {
                
               
                int j = 0;
                if (!tupleSpace.Keys.Contains(k))
                    continue;
                foreach (List<Field> ls in tupleSpace[k])
                {
                    if (!(ls.Count == l.Count))
                        continue;
                    bool eq = true;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (aux[i].equals(ls[i]))
                            continue;
                        else
                        {
                            eq = false;
                            break;

                        }

                    }
                    if (eq)
                    {
                        aux2.Add(ls);
                        takes.Add(j);
                        founded = 1;
                        continue;
                    }
                    j++;
                }
                foreach (int w in takes)
                    tupleSpace[k].RemoveAt(w);
                if (tupleSpace[k].Count == 0)
                    tupleSpace.Remove(k);
            }
                if (founded == 1)
                return aux2;
            else
                return null;
        }

        public static object funcArgs(string splited)
        {
            object args;
            int j;
            if (splited.StartsWith("\"") && splited.EndsWith("\""))
            {       
                args = splited;
            }
            else
            {
                try
                {
                    Int32.TryParse(splited, out j);
                    args = j;
                }
                catch (FormatException) { return null; }
            }
            return args;
        }

        public List<List<Field>> selectTuple(List<string> l)
        {
            Random r = new Random();
            int rInt = r.Next(min_delay, max_delay); //for ints
            System.Threading.Thread.Sleep(rInt);

            List<Field> aux = new List<Field>();
            List<List<Field>> aux2 = new List<List<Field>>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            List<int> takes = new List<int>();

            if (l == null)
                return null;
            string first = l[0];
            int founded = 0;
            string key;

            if (!first.Contains("("))
                key = first.Replace("\"", "");
            else
            {
                string[] key2 = first.Split(new char[] { '(', ')', ',' });
                key = key2[0];

            }


            foreach (string s in l)
            {
                if (!s.Contains("("))
                {
                    if (s.Contains("\"") || s.Equals("null"))
                        aux.Add(new Field(s.Replace("\"", "")));
                    else
                    {
                        Type type = assembly.GetType("RemotingSample." + s);
                        aux.Add(new Field(Activator.CreateInstance(type), 2));
                    }
                }
                else
                {
                    string[] splited = s.Split(new char[] { '(', ')', ',' });
                    string func = splited[0];
                    object[] o = new object[splited.Length - 2];
                    for (int i = 1; i < splited.Length - 1; i++)
                        o[i - 1] = funcArgs(splited[i]);
                    Type type = assembly.GetType("RemotingSample." + func);
                    aux.Add(new Field(Activator.CreateInstance(type, o), 0));


                }

            }

            List<string> keys = new List<string>();
            if (key.Equals("null") || key.Equals("*"))
            {
                keys = tupleSpace.Keys.ToList();
            }
            else if (key.Contains("*"))
            {
                string[] subKey = key.Split('*');
                if (key.StartsWith("*"))
                {
                    foreach (string k in tupleSpace.Keys)
                    {
                        if (k.EndsWith(subKey[1]))
                            key = k;
                    }
                    keys.Add(key);
                }
                else if (key.EndsWith("*"))
                {
                    foreach (string k in tupleSpace.Keys)
                    {
                        if (k.StartsWith(subKey[1]))
                            key = k;
                    }
                    keys.Add(key);
                }
            }
            else keys.Add(key);

            foreach (string k in keys)
            {


                int j = 0;
                if (!tupleSpace.Keys.Contains(k))
                    continue;
                foreach (List<Field> ls in tupleSpace[k])
                {
                    if (!(ls.Count == l.Count))
                        continue;
                    bool eq = true;
                    for (int i = 0; i < ls.Count; i++)
                    {
                        if (aux[i].equals(ls[i]))
                            continue;
                        else
                        {
                            eq = false;
                            break;

                        }

                    }
                    if (eq)
                    {
                        aux2.Add(ls);
                        takes.Add(j);
                        founded = 1;
                        continue;
                    }
                    j++;
                }
            }
            if (founded == 1)
                return aux2;
            else
                return null;
        }
    }


    public class DADTestA
        {
            public int i1;
            public string s1;

            public DADTestA() { }
            public DADTestA(int pi1, string ps1)
            {
                i1 = pi1;
                s1 = ps1;
            }
        public override string ToString()
        {
           return this.i1 + "    " + this.s1;
        }
            public bool Equals(DADTestA o)
            {
                if (o == null)
                {
                return false;
                }
                else
                {
                    return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)));
                }
            }
        }

        public class DADTestB
    {
            public int i1;
            public string s1;
            public int i2;
            public DADTestB() { }
            public DADTestB(int pi1, string ps1, int pi2)
            {
                i1 = pi1;
                s1 = ps1;
                i2 = pi2;
            }

            public bool Equals(DADTestB o)
            {
                if (o == null)
                {
                    return false;
                }
                else
                {
                    return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.i2 == o.i2));
                }
            }
        }

        public class DADTestC
    {
            public int i1;
            public string s1;
            public string s2;

            public DADTestC() { }
            public DADTestC(int pi1, string ps1, string ps2)
            {
                i1 = pi1;
                s1 = ps1;
                s2 = ps2;
            }

            public bool Equals(DADTestC o)
            {
                if (o == null)
                {
                    return false;
                }
                else
                {
                    return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.s2.Equals(o.s2)));
                }
            }
        }

    public class Field : MarshalByRefObject
    {
        private int type = 0;
        private string s;
        private object test;

        public Field(string s2)
        {
            type = 1;
            s = s2;
        }

        public Field(object v, int i)
        {
            test = v;
            type = i;
        }

        public object getTest()
        {
            return test;
        }

        public string getString()
        {
            return s;
        }

        public int getType()
        {
            return type;
        }
        
        public string getClassName()
        {
            return test.GetType().Name;
        }

        public bool equals(Field f)
        {
            if (type == 1)
            {
                if (s.Equals("*") && f.getType() == 1)
                    return true;
                if(s.Equals("null") && f.getType() == 0)
                    return true;
                if (s.Contains("*") && f.getType() == 1)
                {
                    String[] sTest = s.Split('*');
                    if (s.StartsWith("*"))
                    {
                        if (f.getString().EndsWith(sTest[0]))
                            return true;
                    }
                    else if (s.EndsWith("*"))
                    {
                        if (f.getString().StartsWith(sTest[0]))
                            return true;
                    }
                    else
                    {
                        return false;
                    }
                } 
                return s.Equals(f.getString());
            }
            else
            {
                if (test.GetType().Name.Equals(f.getClassName()))
                {
                    if (type == 2)
                    {
                        return true;
                    }
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    Type typ = test.GetType();
                    MethodInfo mt = typ.GetMethod("Equals", new Type[] { typ });
                    object[] o = new object[1];
                    o[0] = f.getTest();
                    object result = mt.Invoke(test, o);    
                    return (bool)result;
                }
                else
                {
                    return false;
                }
            }
        }
    }
    

    public interface MyRemoteInterface
    {
        string MetodoOla();
        int add(List<string> l);
        List<List<Field>> readTuple(List<string> l);
        List<List<Field>> take(List<string> l);

        List<List<Field>> selectTuple(List<string> l);


    }
}