using Louis.CustomPackages.CommandLineInterface.Core;
using NUnit.Framework;

public class BindingTests {
    [Test]
    public void BindsStringArrayWithEscapedQuotes() {
        var schema = new CommandSchema().Required<string[]>("items", 0);
        var tokens = CommandTokenizer.Tokenize(@"test items=[""hello"", ""say \""Hi\"" now""]");
        var args = CommandBinder.Bind(schema, new ParsedTokens(tokens[1..], schema.Shorthands));
        CollectionAssert.AreEqual(new[] { "hello", "say \"Hi\" now" }, args.Get<string[]>("items"));
    }

    [Test]
    public void BindsBooleanArray() {
        var schema = new CommandSchema().Required<bool[]>("items", 0);
        var tokens = CommandTokenizer.Tokenize("test items=[true,false]");
        var args = CommandBinder.Bind(schema, new ParsedTokens(tokens[1..], schema.Shorthands));
        CollectionAssert.AreEqual(new[] { true, false }, args.Get<bool[]>("items"));
    }

    [Test]
    public void BindsFloatArray() {
        var schema = new CommandSchema().Required<float[]>("items", 0);
        var tokens = CommandTokenizer.Tokenize("test items=[1.5,-2.25]");
        var args = CommandBinder.Bind(schema, new ParsedTokens(tokens[1..], schema.Shorthands));
        CollectionAssert.AreEqual(new[] { 1.5f, -2.25f }, args.Get<float[]>("items"));
    }

    [TestCase(@"say ""He said \""hello\""; # [stay]""", "He said \"hello\"; # [stay]")]
    [TestCase(@"say dialogue=""C:\\Temp\file""", @"C:\Temp\file")]
    public void BindsEscapedQuotedString(string command, string expected) {
        var schema = new CommandSchema().Required<string>("dialogue", 0);
        var tokens = CommandTokenizer.Tokenize(command);
        var args = CommandBinder.Bind(schema, new ParsedTokens(tokens[1..], schema.Shorthands));
        Assert.AreEqual(expected, args.Get<string>("dialogue"));
    }

    [System.Serializable]
    public struct ChoiceOption {
        public string buttonText;
        public int stepIndex;
    }

    [Test]
    public void BindsMultilineChoiceOptions() {
        var schema = new CommandSchema().Required<ChoiceOption[]>("options", 0);
        string command = "multiChoice options=[\n" +
            "{\"buttonText\": \"Loop to start\", \"stepIndex\": 0},\n" +
            "{\"buttonText\": \"Go to end\", \"stepIndex\": 2}\n]";
        var tokens = CommandTokenizer.Tokenize(command);
        var args = CommandBinder.Bind(schema, new ParsedTokens(tokens[1..], schema.Shorthands));
        var options = args.Get<ChoiceOption[]>("options");
        Assert.AreEqual(2, options.Length);
        Assert.AreEqual("Loop to start", options[0].buttonText);
        Assert.AreEqual(0, options[0].stepIndex);
        Assert.AreEqual("Go to end", options[1].buttonText);
        Assert.AreEqual(2, options[1].stepIndex);
    }

    [Test]
    public void BindsEmptyArray() {
        var schema = new CommandSchema().Required<int[]>("items", 0);
        var tokens = new ParsedTokens(new[] { "items=[]" }, schema.Shorthands);
        Assert.IsEmpty(CommandBinder.Bind(schema, tokens).Get<int[]>("items"));
    }

    [Test]
    public void RejectsObjectForArrayArgument() {
        var schema = new CommandSchema().Required<int[]>("items", 0);
        var tokens = new ParsedTokens(new[] { "items={}" }, schema.Shorthands);
        Assert.Throws<CommandArgumentException>(() => CommandBinder.Bind(schema, tokens));
    }

    [Test]
    public void TestingArrayBinding() {
        CommandSchema schema = new CommandSchema()
            .Required<int[]>("param", 0);
        string cmd = "test [1,2,3]";

        // Parsing Process
        var tokens= CommandTokenizer.Tokenize(cmd);
        var parameters = tokens[1..];
        var parsedTokens = new ParsedTokens(parameters, schema.Shorthands);
        var boundArgs = CommandBinder.Bind(schema, parsedTokens);

        Assert.IsInstanceOf<int[]>(boundArgs.Get<int[]>("param"));
        Assert.IsNotEmpty(boundArgs.Get<int[]>("param"));
        Assert.AreEqual(3, boundArgs.Get<int[]>("param").Length);
        Assert.AreEqual(1, boundArgs.Get<int[]>("param")[0]);
        Assert.AreEqual(2, boundArgs.Get<int[]>("param")[1]);
        Assert.AreEqual(3, boundArgs.Get<int[]>("param")[2]);
    }

    [Test]
    public void TestingJsonBinding() {
        CommandSchema schema = new CommandSchema()
            .Required<TestStruct>("person", 0);

        string cmd = "test person={\"firstname\":\"Bob\",\"surname\":\"Smith\"}";

        // Parsing Process
        var tokens = CommandTokenizer.Tokenize(cmd);
        var parameters = tokens[1..];
        var parsedTokens = new ParsedTokens(parameters, schema.Shorthands);
        var boundArgs = CommandBinder.Bind(schema, parsedTokens);


        object person = boundArgs.Get<TestStruct>("person");
        Assert.IsInstanceOf<TestStruct>(person);
        Assert.AreEqual("Bob", ((TestStruct)person).firstname);
        Assert.AreEqual("Smith", ((TestStruct)person).surname);
    }

    [Test]
    public void TestingMultiLineBinding() {
        CommandSchema schema = new CommandSchema()
            .Required<TestStruct[]>("people", 0);

        string cmd = "test people=[{\"firstname\":\"Bob\",\"surname\":\"Smith\"},{\"firstname\":\"Jane\",\"surname\":\"Doe\"}]";
        var tokens = CommandTokenizer.Tokenize(cmd);
        var parameters = tokens[1..];
        var parsedTokens = new ParsedTokens(parameters, schema.Shorthands);
        var boundArgs = CommandBinder.Bind(schema, parsedTokens);


        object people = boundArgs.Get<TestStruct[]>("people");
        Assert.IsInstanceOf<TestStruct[]>(people);
        TestStruct[] converted = (TestStruct[]) people;

        Assert.AreEqual("Bob", converted[0].firstname);
        Assert.AreEqual("Smith", converted[0].surname);
        Assert.AreEqual("Jane", converted[1].firstname);
        Assert.AreEqual("Doe", converted[1].surname);
    }

    [Test]
    public void TestingEnumBinding() {
        CommandSchema schema = new CommandSchema()
            .Required<TestEnum>("mode", 0);

        string cmd1 = "test mode=AlwaysOpen";
        string cmd2 = "test mode=AlwaysClosed";
        string cmd3 = "test mode=OpenOnMessage";

        var tokens1 = CommandTokenizer.Tokenize(cmd1);
        var tokens2 = CommandTokenizer.Tokenize(cmd2);
        var tokens3 = CommandTokenizer.Tokenize(cmd3);

        var params1 = tokens1[1..];
        var params2 = tokens2[1..];
        var params3 = tokens3[1..];

        var parsed1 = new ParsedTokens(params1, schema.Shorthands);
        var parsed2 = new ParsedTokens(params2, schema.Shorthands);
        var parsed3 = new ParsedTokens(params3, schema.Shorthands);

        var bound1 = CommandBinder.Bind(schema, parsed1);
        var bound2 = CommandBinder.Bind(schema, parsed2);
        var bound3 = CommandBinder.Bind(schema, parsed3);

        Assert.AreEqual(TestEnum.AlwaysOpen, bound1.Get<TestEnum>("mode"));
        Assert.AreEqual(TestEnum.AlwaysClosed, bound2.Get<TestEnum>("mode"));
        Assert.AreEqual(TestEnum.OpenOnMessage, bound3.Get<TestEnum>("mode"));
    }

    [Test]
    public void TestingStringBinding() {
        CommandSchema schema = new CommandSchema()
            .Required<string>("message", 0);
        string cmd1 = "test \"Hello, World!\"";
        string cmd2 = "test message=\"Hello, World!\"";

        var tokens1 = CommandTokenizer.Tokenize(cmd1);
        var tokens2 = CommandTokenizer.Tokenize(cmd2);

        var parameters1 = tokens1[1..];
        var parameters2 = tokens2[1..];

        var parsedTokens1 = new ParsedTokens(parameters1, schema.Shorthands);
        var parsedTokens2 = new ParsedTokens(parameters2, schema.Shorthands);

        var boundArgs1 = CommandBinder.Bind(schema, parsedTokens1);
        var boundArgs2 = CommandBinder.Bind(schema, parsedTokens2);

        Assert.AreEqual("Hello, World!", boundArgs1.Get<string>("message"));
        Assert.AreEqual("Hello, World!", boundArgs2.Get<string>("message"));
    }

    [TestCase("-1 -1", -1f, -1f)]
    [TestCase("-0.5 -2.25", -0.5f, -2.25f)]
    [TestCase("-.5 -1e3", -0.5f, -1000f)]
    [TestCase("-1e-3 2", -0.001f, 2f)]
    [TestCase("x=-1 y=-1", -1f, -1f)]
    [TestCase("\"-1\" \"-1\"", -1f, -1f)]
    [SetCulture("en-US")]
    public void NegativeCoordinatesBindWithoutUsingOptionalDefaults(string arguments, float x, float y) {
        var schema = new CommandSchema()
            .Optional<float>("x", 0, 0f)
            .Optional<float>("y", 1, 0f);
        var tokens = CommandTokenizer.Tokenize(arguments);
        var parsed = new ParsedTokens(tokens, schema.Shorthands);
        var bound = CommandBinder.Bind(schema, parsed);

        Assert.AreEqual(x, bound.Get<float>("x"));
        Assert.AreEqual(y, bound.Get<float>("y"));
        Assert.IsEmpty(parsed.Flags);
    }

    [TestCase("en-US")]
    [TestCase("fr-FR")]
    public void NegativeNumbersRemainPositionalAlongsideFlags(string culture) {
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
            var aliases = new System.Collections.Generic.Dictionary<string, string> { { "d", "default" } };
            var parsed = new ParsedTokens(CommandTokenizer.Tokenize("-0.5 -1e3 -d --verbose -custom x=-2"), aliases);
            CollectionAssert.AreEqual(new[] { "-0.5", "-1e3" }, parsed.Positional);
            CollectionAssert.AreEquivalent(new[] { "default", "verbose", "custom" }, parsed.Flags);
            Assert.AreEqual("-2", parsed.Named["x"]);
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
        }
    }

    struct TestStruct {
        public string firstname;
        public string surname;
    }

    enum TestEnum {
        AlwaysOpen,
        AlwaysClosed,
        OpenOnMessage
    }
}
