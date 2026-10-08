using System.Collections.Generic;

namespace RepoCity
{
    public class CodeTestCase
    {
        public string setupCode;
        public string expectedOutput;
    }

    public class CodeChallenge
    {
        public string id;
        public string title;
        public string prompt;
        public string starterCode;
        public List<CodeTestCase> tests;
    }

    // Beginner programming-fundamentals challenges for the Programming Central kiosk.
    // A test runs (testCase.setupCode + "\n" + playerCode) through MiniLang.Run and
    // compares the trimmed, newline-joined stdoutLines against testCase.expectedOutput.
    public static class CodeChallenges
    {
        public static List<CodeChallenge> All()
        {
            return new List<CodeChallenge>
            {
                FizzBuzz(),
                SumToN(),
                PrimeCheck(),
                ReverseString(),
                CountVowels(),
                MaxOfFour(),
            };
        }

        static CodeChallenge FizzBuzz()
        {
            return new CodeChallenge
            {
                id = "fizzbuzz",
                title = "FizzBuzz",
                prompt = "Print the numbers from 1 to n, one per line. For multiples of 3 print \"Fizz\", for multiples of 5 print \"Buzz\", and for multiples of both print \"FizzBuzz\" instead of the number.",
                starterCode =
                    "i = 1;\n" +
                    "while (i <= n) {\n" +
                    "    // TODO: print Fizz/Buzz/FizzBuzz instead of i where it applies\n" +
                    "    print(i);\n" +
                    "    i = i + 1;\n" +
                    "}\n",
                tests = new List<CodeTestCase>
                {
                    new CodeTestCase
                    {
                        setupCode = "n = 15;",
                        expectedOutput = "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz",
                    },
                    new CodeTestCase
                    {
                        setupCode = "n = 5;",
                        expectedOutput = "1\n2\nFizz\n4\nBuzz",
                    },
                },
            };
        }

        static CodeChallenge SumToN()
        {
            return new CodeChallenge
            {
                id = "sum-to-n",
                title = "Sum 1 to N",
                prompt = "Compute the sum of all whole numbers from 1 to n (inclusive) and print the result.",
                starterCode =
                    "i = 1;\n" +
                    "total = 0;\n" +
                    "while (i <= n) {\n" +
                    "    // TODO: add i to total\n" +
                    "    i = i + 1;\n" +
                    "}\n" +
                    "print(total);\n",
                tests = new List<CodeTestCase>
                {
                    new CodeTestCase { setupCode = "n = 10;", expectedOutput = "55" },
                    new CodeTestCase { setupCode = "n = 1;", expectedOutput = "1" },
                },
            };
        }

        static CodeChallenge PrimeCheck()
        {
            return new CodeChallenge
            {
                id = "prime-check",
                title = "Prime Checker",
                prompt = "Given a number x, print true if it is prime (greater than 1, divisible by nothing other than 1 and itself) and false otherwise.",
                starterCode =
                    "n = x;\n" +
                    "isPrime = true;\n" +
                    "if (n < 2) {\n" +
                    "    isPrime = false;\n" +
                    "}\n" +
                    "i = 2;\n" +
                    "while (i < n) {\n" +
                    "    // TODO: if n is divisible by i, set isPrime to false\n" +
                    "    i = i + 1;\n" +
                    "}\n" +
                    "print(isPrime);\n",
                tests = new List<CodeTestCase>
                {
                    new CodeTestCase { setupCode = "x = 17;", expectedOutput = "true" },
                    new CodeTestCase { setupCode = "x = 12;", expectedOutput = "false" },
                },
            };
        }

        static CodeChallenge ReverseString()
        {
            return new CodeChallenge
            {
                id = "reverse-string",
                title = "Reverse a String",
                prompt = "Given a string s, print it reversed. Use len(s) to get the length and charAt(s, i) to read the character at a 0-based index.",
                starterCode =
                    "result = \"\";\n" +
                    "i = len(s) - 1;\n" +
                    "while (i >= 0) {\n" +
                    "    // TODO: append charAt(s, i) to result\n" +
                    "    i = i - 1;\n" +
                    "}\n" +
                    "print(result);\n",
                tests = new List<CodeTestCase>
                {
                    new CodeTestCase { setupCode = "s = \"hello\";", expectedOutput = "olleh" },
                    new CodeTestCase { setupCode = "s = \"ab\";", expectedOutput = "ba" },
                },
            };
        }

        static CodeChallenge CountVowels()
        {
            return new CodeChallenge
            {
                id = "count-vowels",
                title = "Count Vowels",
                prompt = "Given a string s, count and print how many of its characters are vowels (a, e, i, o, u, upper or lower case).",
                starterCode =
                    "count = 0;\n" +
                    "i = 0;\n" +
                    "while (i < len(s)) {\n" +
                    "    c = charAt(s, i);\n" +
                    "    // TODO: if c is a vowel, add 1 to count\n" +
                    "    i = i + 1;\n" +
                    "}\n" +
                    "print(count);\n",
                tests = new List<CodeTestCase>
                {
                    new CodeTestCase { setupCode = "s = \"hello world\";", expectedOutput = "3" },
                    new CodeTestCase { setupCode = "s = \"xyz\";", expectedOutput = "0" },
                },
            };
        }

        static CodeChallenge MaxOfFour()
        {
            return new CodeChallenge
            {
                id = "max-of-four",
                title = "Biggest of Four",
                prompt = "Given four numbers a, b, c, d, print the largest one.",
                starterCode =
                    "max = a;\n" +
                    "// TODO: compare b, c and d against max and update it when one is bigger\n" +
                    "print(max);\n",
                tests = new List<CodeTestCase>
                {
                    new CodeTestCase { setupCode = "a = 3;\nb = 9;\nc = 7;\nd = 5;", expectedOutput = "9" },
                    new CodeTestCase { setupCode = "a = 10;\nb = 2;\nc = 2;\nd = 2;", expectedOutput = "10" },
                },
            };
        }
    }
}
