import os
import re

tests_dir = r"D:\منظومة انجاز 2026\Enjaz.Tests"

for root, _, files in os.walk(tests_dir):
    for f in files:
        if f.endswith('.cs'):
            path = os.path.join(root, f)
            try:
                with open(path, 'r', encoding='utf-8-sig') as f_read:
                    text = f_read.read()
            except UnicodeDecodeError:
                with open(path, 'r', encoding='cp1256') as f_read:
                    text = f_read.read()


            text = text.replace('using NUnit.Framework;', 'using Xunit;')
            text = text.replace('[TestFixture]', '')
            text = text.replace('[Test]', '[Fact]')
            text = text.replace('Assert.AreEqual', 'Assert.Equal')
            text = text.replace('Assert.IsNotNull', 'Assert.NotNull')
            text = text.replace('Assert.IsNull', 'Assert.Null')
            text = text.replace('Assert.IsFalse', 'Assert.False')
            text = text.replace('Assert.IsTrue', 'Assert.True')
            text = text.replace('Assert.Pass()', '// pass')
            text = text.replace('Assert.Fail', 'Assert.True(false, ')
            
            # Change SetUp to constructor
            class_name_match = re.search(r'public class (\w+Tests)', text)
            if class_name_match:
                class_name = class_name_match.group(1)
                text = re.sub(r'\[SetUp\]\s*public void Setup\(\)', f'public {class_name}()', text)
                text = re.sub(r'\[OneTimeSetUp\]\s*public void Setup\(\)', f'public {class_name}()', text)
            
            # Fix Database connection string bug in SystemSimulationTests
            if 'SystemSimulationTests.cs' in path:
                text = text.replace('_dbService = new DatabaseService(_testDbPath);', '_dbService = new DatabaseService($"Data Source={_testDbPath}");')
                # Remove [Order] attributes which xUnit doesn't support easily without custom runners
                text = re.sub(r'\[Test, Order\(\d+\)\]', '[Fact]', text)
                text = re.sub(r'\[Category\("[^"]+"\)\]', '', text)
                
            
            with open(path, 'w', encoding='utf-8') as f_write:
                f_write.write(text)

        elif f.endswith('.csproj'):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as f_read:
                text = f_read.read()
            
            text = text.replace('<PackageReference Include="NUnit" Version="3.14.0" />', '<PackageReference Include="xunit" Version="2.6.5" />')
            text = text.replace('<PackageReference Include="NUnit.Analyzers" Version="3.9.0" />', '')
            text = text.replace('<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />', '<PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />\n    <PackageReference Include="MSTest.TestAdapter" Version="3.1.1" />')
            text = text.replace('<Using Include="NUnit.Framework" />', '<Using Include="Xunit" />')
            
            with open(path, 'w', encoding='utf-8') as f_write:
                f_write.write(text)

print('Conversion Complete')
