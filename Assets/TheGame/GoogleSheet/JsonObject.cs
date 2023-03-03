using System.Collections.Generic;

namespace TheGame.GoogleSheet
{
    public class JsonObject
    {
        private Dictionary<string, List<JsonObject>> _arrays;
        private Dictionary<string, string> _parameters;

        public void Add(HeaderMeta meta, string value)
        {
            if (meta.isArrayElement)
            {
                _arrays ??= new Dictionary<string, List<JsonObject>>();
                
                if (!_arrays.TryGetValue(meta.arrayName, out List<JsonObject> array))
                {
                    array = new List<JsonObject>();
                    _arrays[meta.arrayName] = array;
                }

                int missingElementCount = meta.arrayElementIndex - array.Count + 1;
                
                for (int i = 0; i < missingElementCount; i++)
                {
                    array.Add(new JsonObject());
                }

                HeaderMeta arrayElementMeta = new ()
                {
                    parameterName = meta.parameterName
                };

                array[meta.arrayElementIndex].Add(arrayElementMeta, value);
            }
            else
            {
                _parameters ??= new Dictionary<string, string>();
                _parameters[meta.parameterName] = value;
            }
        }

        public void Print(JsonOutput output)
        {
            PrintTabs(output);
            PrintChar(output, '{');
            PrintLine(output);

            int filledArrayCount = 0;
            if (_arrays != null)
            {
                foreach ((string arrayName, List<JsonObject> array) in _arrays)
                {
                    foreach (JsonObject jsonObject in array)
                    {
                        if (jsonObject.HasAnyData())
                        {
                            filledArrayCount++;
                            break;
                        }
                    }
                }
            }

            output.depth++;
            
            int counter = 0;
            foreach ((string paramName, string paramValue) in _parameters)
            {
                PrintTabs(output);
                
                PrintParameter(output, paramName, paramValue);

                if (counter + 1 < _parameters.Count || filledArrayCount > 0)
                {
                    PrintChar(output, ',');
                }

                PrintLine(output);

                counter++;
            }

            if (filledArrayCount > 0)
            {
                counter = 0;
                
                foreach ((string arrayName, List<JsonObject> array) in _arrays)
                {
                    PrintArray(output, arrayName, array);

                    if (counter + 1 < filledArrayCount)
                    {
                        PrintChar(output, ',');
                    }

                    PrintLine(output);

                    counter++;
                }
            }

            output.depth--;

            PrintTabs(output);
            PrintChar(output, '}');
        }

        public static void PrintArray(JsonOutput output, string arrayName, List<JsonObject> array)
        {
            if (!string.IsNullOrEmpty(arrayName))
            {
                PrintTabs(output);
                
                PrintChar(output, '"');
                PrintText(output, arrayName);
                PrintChar(output, '"');

                PrintChar(output, ':');
                PrintSpace(output);
            }

            PrintChar(output, '[');
            PrintLine(output);
            output.depth++;

            int filledElementCount = 0;
            foreach (JsonObject jsonObject in array)
            {
                if (jsonObject.HasAnyData())
                {
                    filledElementCount++;
                }
            }

            int counter = 0;
            foreach (JsonObject jsonObject in array)
            {
                if (!jsonObject.HasAnyData())
                {
                    continue;
                }

                jsonObject.Print(output);

                if (counter + 1 < filledElementCount)
                {
                    PrintChar(output, ',');
                    PrintLine(output);
                }

                counter++;
            }

            output.depth--;
            
            PrintLine(output);

            if (!string.IsNullOrEmpty(arrayName))
            {
                PrintTabs(output);
            }

            PrintChar(output, ']');
        }
        
        private static void PrintParameter(JsonOutput output, string name, string value)
        {
            PrintChar(output, '"');
            PrintText(output, name);
            PrintChar(output, '"');
            
            PrintChar(output, ':');
            PrintSpace(output);
            
            PrintChar(output, '"');
            PrintText(output, value);
            PrintChar(output, '"');
        }

        private static void PrintTabs(JsonOutput output)
        {
            for (int i = 0; i < output.depth; i++)
            {
                PrintText(output, output.tab);
            }
        }

        private static void PrintSpace(JsonOutput output)
        {
            PrintText(output, output.space);
        }

        private static void PrintLine(JsonOutput output)
        {
            PrintText(output, output.lineSeparator);
        }
        
        private static void PrintChar(JsonOutput output, char character)
        {
            output.stream.Append(character);
        }
        
        private static void PrintText(JsonOutput output, string text)
        {
            output.stream.Append(text);
        }

        private bool HasAnyData()
        {
            return _parameters?.Count > 0 || _arrays?.Count > 0;
        }
    }
}