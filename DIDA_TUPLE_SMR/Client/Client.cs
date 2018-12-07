using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Runtime.Serialization.Formatters;
using System.Net;
using System.Threading;
using System.Runtime.CompilerServices;

namespace RemotingSample {

    public class Client
    {

        private string id;
        private Uri uri;
        private string fileName;
        bool crash = false;
        private static List<Uri> urls;
        private static List<string> names;

        public Client(string n, Uri uri2, string file, List<Uri> urls2, List<string> names2)
        {
            id = n;
            fileName = file;
            uri = uri2;
            urls = new List<Uri>(urls2);
            names = names2;
        }

        public void setCrash(bool c)
        {
            crash = c;
        }

        public void executeByPuppet()
        {
            int urlRead = 0;
            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            string name = "s";
            int port = 8086;
            if (uri != null)
            {
                props["port"] = uri.Port;
                port = uri.Port;
            }
            else
                props["port"] = 8086;
            if (id != null)
            {
                props["name"] = id;
                name = id;
            }
            TcpChannel channel = new TcpChannel(props, null, provider);
            ChannelServices.RegisterChannel(channel, false);
            MyRemoteInterface obj=null;
            while (obj == null)
            {
                try
                {

                    String reference = "tcp://localhost:" + urls[urlRead].Port + "/MyRemoteObjectName/" + names[urlRead];
                    obj = (MyRemoteInterface)Activator.GetObject(
                    typeof(MyRemoteInterface), reference);
                    if (obj == null)
                        urlRead++;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ReadLine();
                }

            }
            executeMain(obj, 1, fileName, urlRead);
            
        }
        public static string GetIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        public void status()
        {
            if (crash)
                Console.WriteLine("Client " + id + " crashed status");
            else
                Console.WriteLine("Client " + id + " available");
        }

        public static void repeatCmd(MyRemoteInterface obj, List<List<string>> field_list2, List<string> cmds)
        {
            int delay = 0;
            List<List<Field>> lField;
            for (int i=0;i<cmds.Count;i++)
            {
                
                switch (cmds[i])
                {
                    case "add":
                        obj.add(field_list2[i]);
                        break;
                    case "read":
                        lField = obj.readTuple(field_list2[i]);
                        while (lField == null)
                            lField = obj.readTuple(field_list2[i]);
                        foreach (List<Field> ls in lField)
                            foreach (Field f in ls)
                            {
                                if (f.getType() == 0 || f.getType() == 2)
                                    Console.WriteLine(f.getClassName());
                                else
                                    Console.WriteLine(f.getString());

                            }
                        break;
                    case "take":
                        lField = obj.take(field_list2[i]);
                        while (lField == null)
                            lField = obj.take(field_list2[i]);
                        foreach (List<Field> ls in lField)
                            foreach (Field f in ls)
                            {
                                if (f.getType() == 0 || f.getType() == 2)
                                    Console.WriteLine(f.getClassName());
                                else
                                    Console.WriteLine(f.getString());

                            }
                        break;

                    case "wait":
                        Int32.TryParse(field_list2[i][0], out delay);
                        System.Threading.Thread.Sleep(delay);
                        Console.WriteLine(delay);
                        break;
                }
            }
        }

        public static void executeMain(MyRemoteInterface obj, int args,string arg, int urlRead)
        {
            string str_fields = "";
            List<string> cmds = new List<string>();
            List<string> field_list = new List<string>();
            List<List<string>> field_list2 = new List<List<string>>();
            List<List<Field>> lField;
            string[] cmd_params;
            string cmd = "";
            int delay = 0;
            int repeat = 0;

            if (args == 1)
            {
                string[] lines = File.ReadAllLines(arg);
                foreach (string l in lines)
                {
                    if (l.Length == 0)
                        continue;
                    if (l.Contains("end"))
                        cmd = l;
                    else
                    {
                        cmd_params = l.Split(' ');
                        cmd = cmd_params[0];
                        str_fields = cmd_params[1];

                        if (cmd.Contains("repeat") || cmd.Equals("wait"))
                            Console.WriteLine("status command " + cmd + " " + cmd_params[1]);
                        else
                        {
                            if (cmd_params.Length > 2)
                                return;
                            field_list = argumentParser(str_fields);
                        }
                    }

                    int sent = 0;

                    while (sent == 0)
                    {

                        try
                        {
                            switch (cmd)
                            {
                                case "add":
                                    obj.add(field_list);
                                    if (repeat > 0)
                                    {
                                        field_list2.Add(field_list);
                                        cmds.Add(cmd);
                                    }
                                    sent = 1;
                                    break;
                                case "read":
                                    lField = obj.readTuple(field_list);
                                    if (repeat > 0)
                                    {
                                        field_list2.Add(field_list);
                                        cmds.Add(cmd);
                                    }
                                    while (lField == null)
                                        lField = obj.readTuple(field_list);
                                    foreach (List<Field> ls in lField)
                                        foreach (Field f in ls)
                                        {
                                            if (f.getType() == 0 || f.getType() == 2)
                                                Console.WriteLine(f.getClassName());
                                            else
                                                Console.WriteLine(f.getString());

                                        }
                                    sent = 1;
                                    break;
                                case "take":
                                    if (repeat > 0)
                                    {
                                        field_list2.Add(field_list);
                                        cmds.Add(cmd);
                                    }
                                    lField = obj.take(field_list);
                                    while (lField == null)
                                        lField = obj.take(field_list);
                                    foreach (List<Field> ls in lField)
                                        foreach (Field f in ls)
                                        {
                                            if (f.getType() == 0 || f.getType() == 2)
                                                Console.WriteLine(f.getClassName());
                                            else
                                                Console.WriteLine(f.getString());

                                        }
                                    sent = 1;
                                    break;

                                case "wait":
                                    Int32.TryParse(str_fields, out delay);
                                    System.Threading.Thread.Sleep(delay);
                                    if (repeat > 0)
                                    {
                                        List<string> x = new List<string>();
                                        x.Add(str_fields);
                                        field_list2.Add(x);
                                        cmds.Add(cmd);
                                    }
                                    sent = 1;
                                    break;
                                case "begin-repeat":
                                    Int32.TryParse(str_fields, out repeat);
                                    sent = 1;
                                    break;
                                case "end-repeat":
                                    for (int i = 0; i < (repeat - 1); i++)
                                    {
                                        repeatCmd(obj, field_list2, cmds);
                                    }
                                    repeat = 0;
                                    sent = 1;

                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            if (e is RemotingException || e is SocketException)
                            {
                                Thread.Sleep(7000);
                                urlRead++;
                                Console.WriteLine("SERVER CRASHED, REPLACING REMOTE OBJECT");
                                String reference = "";
                                bool set = false;
                                while(!set)
                                {
                                    reference = "tcp://localhost:" + urls[urlRead].Port + "/MyRemoteObjectName/" + names[urlRead];
                                    obj = (MyRemoteInterface)Activator.GetObject(
                                    typeof(MyRemoteInterface), reference);
                                    Console.WriteLine("Trying:  " + reference);
                                    if (obj == null)
                                    {
                                        urlRead++;
                                        continue;
                                    }
                                    else
                                    {
                                        set = true;
                                        break;
                                    }
                                }
                            }

                        }
                    }
                    sent = 0;
                }
            }
            else
            {
                try
                {
                    while (true)
                    {

                        
                        Console.WriteLine("Write a command add|take|read <field1,field2,...,fieldn>  (exit:to leave) :");
                        string l = Console.ReadLine();
                        if (l.Length == 0)
                            continue;
                        cmd_params = l.Split(' ');
                        cmd = cmd_params[0];
                        str_fields = cmd_params[1].Replace("\n", "");
                        field_list = argumentParser(str_fields);
                        if (cmd.Equals("exit"))
                        {
                            Console.WriteLine("Exiting");
                            break;
                        }

                        switch (cmd)
                        {
                            case "add":
                                obj.add(field_list);
                                break;
                            case "read":

                                lField = obj.readTuple(field_list);
                                while (lField == null)
                                    lField = obj.readTuple(field_list);
                                foreach (List<Field> ls in lField)
                                    foreach (Field f in ls)
                                    {
                                        if (f.getType() == 0)
                                            Console.WriteLine(f.getClassName());
                                        else
                                            Console.WriteLine(f.getString());

                                    }
                                break;
                            case "take":
                                lField = obj.take(field_list);
                                while (lField == null)
                                    lField = obj.readTuple(field_list);
                                foreach (List<Field> ls in lField)
                                    foreach (Field f in ls)
                                    {
                                        if (f.getType() == 0)
                                            Console.WriteLine(f.getClassName());
                                        else
                                            Console.WriteLine(f.getString());

                                    }
                                break;
                            
                            default: Console.WriteLine("Invalid Command : {0}. Please,use the correct command syntax.", cmd); break;
                        }
                        
                    }
                }
                catch (SocketException)
                {
                    System.Console.WriteLine("Could not locate server");
                }
                catch (ArgumentOutOfRangeException)
                {
                    System.Console.WriteLine("bad input format1\nadd|take|read <field1,field2,...,fieldn>\nEx: add <\"ola\">");
                    executeMain(obj, args, arg,urlRead);
                }
                catch (IndexOutOfRangeException)
                {
                    System.Console.WriteLine("bad input format2\nadd|take|read <field1,field2,...,fieldn>\nEx: add <\"ola\">");
                    executeMain(obj, args, arg,urlRead);
                }
                catch (RemotingException e)
                {
                    System.Console.WriteLine("OBJJJJ");
                    Console.WriteLine(e.GetType());
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine(e.Message);
                    obj = (MyRemoteInterface)Activator.GetObject(
                    typeof(MyRemoteInterface),
                    "tcp://localhost:8086/MyRemoteObjectName/s");
                    executeMain(obj, args, arg, urlRead);
                }
            }
            
        }

        public void setNames(List<string> list)
        {
            names = new List<string>(list);
        }

        public void setUrls(List<Uri> list)
        {
            urls = new List<Uri>(list);
        }

        static void Main(string[] args)
        {

            try
            {
                BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
                provider.TypeFilterLevel = TypeFilterLevel.Full;
                IDictionary props = new Hashtable();
                props["port"] = 0;
                props["name"] = "c";
                TcpChannel channel = new TcpChannel(props, null, provider);
                ChannelServices.RegisterChannel(channel, false);
                MyRemoteInterface obj = (MyRemoteInterface)Activator.GetObject(
                typeof(MyRemoteInterface),
                "tcp://localhost:8086/MyRemoteObjectName/s");
                if (obj == null)
                    System.Console.WriteLine("Could not locate server");

                if (args.Length == 0)
                    executeMain(obj, 0, "", 0);
                else if (args.Length == 0)
                {
                    executeMain(obj, 1, args[0], 0);
                }
                else
                    return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
        static List<string> argumentParser(string fields)
        {
           
            List<string> argsList = new List<string>();    
            int bracket_counter = 0;
            int quote = 0;
            string cur_field = "";
            int i = 0;
            if (!fields.StartsWith(" < ") && !fields.EndsWith(">"))
            { 
                return null;
            }
            fields = fields.Substring(1, fields.Length - 2);           
            while(i < fields.Length)
            {
                

                if (fields[i] == '"' && quote == 0)
                {
                    cur_field += fields[i];
                    i++;
                    while (fields[i] != '"')
                    {      
                        cur_field += fields[i];
                        i++;
                        quote = 1;
                    }

                    if (quote == 0)
                        return null;
                    cur_field += fields[i];
                    quote = 0;
                    argsList.Add(cur_field);
                    cur_field = "";
                    i++;
                    bracket_counter = 0;

                }

                else if(fields[i] >= 'a' && fields[i] <= 'z' || fields[i] >= 'A' && fields[i] <= 'Z')
                {
                    cur_field += fields[i];
                    i++;
                    while (fields[i] != '(' && fields[i] != ',')
                    {
                        cur_field += fields[i];
                        if (i == (fields.Length - 1))
                        {
                            break;
                        }
                        else
                            i++;
                    }
                    if (fields[i] == ',' || i == (fields.Length - 1))
                    {
                        argsList.Add(cur_field);
                        bracket_counter = 0;
                        cur_field = "";
                        i++;
                    }                   
                    else if (fields[i] == '(')
                    {
                        cur_field += fields[i];
                        i++;
                        string args = "";
                        while (fields[i] != ')')
                        {
                            args += fields[i];
                            i++;
                        }
                        
                        object[] splited = funcArgs(args.Split(','));
                        if (splited == null)
                            return null;
                        cur_field += args + fields[i];
                        argsList.Add(cur_field);
                        cur_field = "";
                        bracket_counter = 0;
                        i++;

                    }
                }
                else if(fields[i] == ',')
                {
                    bracket_counter = 1;
                    i++;
                    continue;
                }
            }     
            if (bracket_counter==0)
                return argsList;
            else
                return null;
        }

        public static object[] funcArgs(string[] splited)
        {
            object[] args = new object[splited.Length];
            int j;
            int i = 0;
            foreach (string field in splited)
            {
                if (field.StartsWith("\"") && field.EndsWith("\""))
                {

                    args[i] = field;
                }
                else
                {
                    try
                    {
                        Int32.TryParse(field, out j);
                        args[i] = j;
                    }
                    catch (FormatException) { return null; }
                }
                i++;
            }
            return args;
        }

    }
}