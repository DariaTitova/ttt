using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace TestApplication
{
    public enum operation { add = 1, multiply, divide, subtract };
    class Program
    {
        static int max_serialazed = 0;
        static string max_serialazed_file = "";

        static void Main(string[] args)
        {
           
            int domainCount = 2;

            Console.WriteLine("введие абсолютный адрес необходимой папки");
            String s = Console.ReadLine();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();


            try
            {

                string[] dirs = Directory.GetFiles(@s, "*.xml");
                int numer_files = dirs.Length, check = 0;

                while (check < numer_files)
                {
                    Thread[] threads = new Thread[domainCount];//потоки
                    for (int i = 0; i < domainCount; i++)
                        if (check + i < numer_files)
                        {
                            threads[i] = new Thread(new ParameterizedThreadStart(Fileread));
                            threads[i].Start(dirs[check + i]);
                        }

                    for (int i = 0; i < domainCount; i++)
                        threads[i].Join();

                    check += domainCount;
                }

                Console.WriteLine("\n '{0}'- файл с наибольшим количеством успешно десериализованных элементов 'calculation'= {1}  ", max_serialazed_file, max_serialazed);

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}.{1:00}", ts.Seconds, ts.Milliseconds / 10);
                Console.WriteLine("\nвремя выполнения программы:    " + elapsedTime);

                Console.Read();
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
                Console.Read();
            }
        }





        public static void Fileread(object obj)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(obj.ToString());

            XmlElement calculations = xDoc.DocumentElement;
            Culculation c = new Culculation(calculations.ChildNodes.Count);//класс для вычисления арифметического выражения

            int count_calculation = 0;

            //calculation
            foreach (XmlNode calculation in calculations)
            {
                int number = 0;
                operation operation = 0;
                string uid = "";
                bool input_number = false, extra_field = false;

                //nodes in calculation
                foreach (XmlNode node in calculation.ChildNodes)
                {
                    if (node.Attributes.GetNamedItem("name").Value == "uid")
                    {
                        uid = node.Attributes.GetNamedItem("value").Value;

                    }
                    else
                        if (node.Attributes.GetNamedItem("name").Value == "operand")
                        {
                            if (!Enum.TryParse(node.Attributes.GetNamedItem("value").Value, out operation))
                            {
                                Console.WriteLine("нестандартное значение 'operand' -> {0}", node.Attributes.GetNamedItem("value").Value);
                                break;
                            }
                        }
                        else
                            if (node.Attributes.GetNamedItem("name").Value == "mod")
                            {
                                if (Int32.TryParse(node.Attributes.GetNamedItem("value").Value, out number))
                                    input_number = true;
                                else
                                {

                                    Console.WriteLine("нечисловое значение 'mod' -> {0}", node.Attributes.GetNamedItem("value").Value);
                                    break;
                                }
                            }
                            else//если лишнее поле
                            {
                                extra_field = true;
                                Console.WriteLine("ошибка. лишнее поле ");
                                break;
                            }

                }

                if ((uid == "" || operation == 0 || !input_number) && !extra_field)
                {
                    Console.Write("ошибка. неполнота данных элемента  «calculation»: пустой элемент/отсутствие: ");
                    if (uid == "") Console.Write("  uid ");
                    if (operation == 0) Console.Write("  operation {0}", operation);
                    if (!input_number) Console.Write("  mod ");
                    Console.WriteLine(" ");
                    break;
                }
                else
                {
                    if (!extra_field)
                    {
                        c.addition(number, operation);
                        count_calculation++;
                    }
                }



            }

            if (max_serialazed < count_calculation)
            {
                max_serialazed = count_calculation;
                max_serialazed_file = Path.GetFileName(obj.ToString());
            }
            Console.WriteLine("имя файла: '{0}' \nрезультат = {1} ", Path.GetFileName(obj.ToString()), c.culculate());
            Console.WriteLine("__________________________________________");
        }
    }


    public class Culculation
    {
        int[] numbers;
        operation[] operations;
        int i;

        public Culculation(int number)
        {
            this.numbers = new int[number + 1];
            numbers[0] = 0;
            this.operations = new operation[number];
            this.i = 0;
        }

        public void addition(int x, operation op)
        {
            numbers[i + 1] = x;
            operations[i] = op;
            i++;
        }

        private bool end = false;
        public int culculate()
        {
            int j = 0;
            while (operations[0] != 0)
            {
                j++;
                if (j >= i) j = 0;

                if (operations[j] == 0) end = true;

                if (operations[j] == operation.divide)
                {
                    shift(j, numbers[j] / numbers[j + 1]);
                    j = 0;
                }
                else
                    if (operations[j] == operation.multiply)
                    {
                        shift(j, numbers[j] * numbers[j + 1]);

                    }
                    else
                        if (operations[j] == operation.add && end)
                        {
                            shift(j, numbers[j] + numbers[j + 1]);
                            j = 0;
                        }
                        else
                            if (operations[j] == operation.subtract && end)
                            {
                                shift(j, numbers[j] - numbers[j + 1]);
                                j = 0;
                            }
            }

            return numbers[0];
        }
        private void shift(int i_, int new_value)
        {
            numbers[i_] = new_value;
            for (int j = i_ + 1; j < i; j++)
            {
                numbers[j] = numbers[j + 1];
                operations[j - 1] = operations[j];
            }
            operations[i - 1] = 0;
            numbers[i] = 0;
        }
    }
}
