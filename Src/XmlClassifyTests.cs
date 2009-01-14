﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;
using NUnit.Framework;

namespace RT.Util.XmlClassify
{
    [TestFixture]
    public class XmlClassifyTests
    {
        private class blankClass
        {
        }

        private class basicClass
        {
            public int AnInt = -123;
            public ushort AUShort = 4747;
            public string AString = "str";
            public bool ABool;
            public ulong AULong;
            public double ADouble = 3.14;
            public DateTime ADateTime;

            public void AssertEqual(basicClass actual)
            {
                Assert.AreEqual(AnInt, actual.AnInt);
                Assert.AreEqual(AUShort, actual.AUShort);
                Assert.AreEqual(AString, actual.AString);
                Assert.AreEqual(ABool, actual.ABool);
                Assert.AreEqual(AULong, actual.AULong);
                Assert.AreEqual(ADouble, actual.ADouble);
                Assert.AreEqual(ADateTime, actual.ADateTime);
            }
        }

        private class classWithDict
        {
            public Dictionary<string, string> Dict = new Dictionary<string, string>();
        }

        private class nestedClass
        {
            public basicClass Basic;
            public nestedClass Nested;

            public void AssertEqual(nestedClass actual)
            {
                Basic.AssertEqual(actual.Basic);
                if (Nested != null && actual.Nested != null)
                    Nested.AssertEqual(actual.Nested);
                else if (Nested == null && actual.Nested != null)
                    Assert.Fail("Nested classes: expected null, actual non-null.");
                else if (Nested != null && actual.Nested == null)
                    Assert.Fail("Nested classes: expected non-null, actual null.");
            }
        }

        private class xmlClass
        {
            public string Str;
            public XElement Xml;
        }

        [Test]
        public void TestBlankClass()
        {
            XElement xel;
            xel = XmlClassify.ObjectToXElement(new blankClass());
            XmlClassify.ObjectFromXElement<blankClass>(xel);
        }

        [Test]
        public void TestBasicClass()
        {
            var clsEx = new basicClass()
            {
                AnInt = -876,
                AUShort = 9876,
                AString = "test String!",
                ABool = true,
                AULong = 9999999999999999999,
                ADouble = Math.PI,
                ADateTime = DateTime.UtcNow,
            };
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<basicClass>(xel);

            clsEx.AssertEqual(clsAc);

            // Double check manually - in this test only.
            Assert.AreEqual(clsEx.AnInt, clsAc.AnInt);
            Assert.AreEqual(clsEx.AUShort, clsAc.AUShort);
            Assert.AreEqual(clsEx.AString, clsAc.AString);
            Assert.AreEqual(clsEx.ABool, clsAc.ABool);
            Assert.AreEqual(clsEx.AULong, clsAc.AULong);
        }

        [Test]
        public void TestClassWithDict()
        {
            var clsEx = new classWithDict();
            clsEx.Dict.Add("abc", "def");
            clsEx.Dict.Add("key", "value");
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<classWithDict>(xel);

            assertDict(clsEx.Dict, clsAc.Dict);
        }

        [Test]
        public void TestClassWithXML()
        {
            var clsEx = new xmlClass()
            {
                Str = "control",
                Xml =
                    new XElement("bla", new XAttribute("attr1", "val1"),
                        new XElement("sub1",
                            new XElement("sub1.1")),
                        new XElement("sub2", new XAttribute("attr2", "val2")))
            };
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<xmlClass>(xel);

            Assert.AreEqual(clsEx.Str, clsAc.Str);
            Assert.AreEqual(clsEx.Xml.ToString(SaveOptions.DisableFormatting), clsAc.Xml.ToString(SaveOptions.DisableFormatting));
        }

        [Test]
        public void TestNestedClass()
        {
            var nestedEx = new nestedClass()
            {
                Basic = new basicClass()
                {
                    AnInt = -5552346,
                    AString = "blah",
                },
                Nested = new nestedClass()
                {
                    Basic = new basicClass()
                    {
                        AString = "deep",
                        ADouble = 1.618,
                    }
                }
            };
            var xel = XmlClassify.ObjectToXElement(nestedEx);
            var nestedAc = XmlClassify.ObjectFromXElement<nestedClass>(xel);

            // Full comparison
            nestedEx.AssertEqual(nestedAc);

            // Sanity checks
            Assert.AreEqual(null, nestedEx.Nested.Nested);
            Assert.AreEqual(-123, nestedEx.Nested.Basic.AnInt);
            Assert.AreEqual(false, nestedEx.Nested.Basic.ABool);

            // Spot checks
            Assert.AreEqual(nestedEx.Basic.AnInt, nestedAc.Basic.AnInt);
            Assert.AreEqual(nestedEx.Basic.AString, nestedAc.Basic.AString);
            Assert.AreEqual(nestedEx.Nested.Basic.AString, nestedAc.Nested.Basic.AString);
            Assert.AreEqual(nestedEx.Nested.Basic.ADouble, nestedAc.Nested.Basic.ADouble);
            Assert.AreEqual(nestedEx.Nested.Nested, nestedAc.Nested.Nested);
        }

        private void assertDict<K, V>(Dictionary<K, V> expected, Dictionary<K, V> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var key in expected.Keys)
            {
                Assert.IsTrue(actual.ContainsKey(key));
                Assert.AreEqual(expected[key], actual[key]);
            }
        }

        [Test]
        public void TestPartialLoad()
        {
            var elem = new XElement("item", new XAttribute("AULong", "987654"));
            var loaded = XmlClassify.ObjectFromXElement<basicClass>(elem);

            Assert.AreEqual(-123, loaded.AnInt);
            Assert.AreEqual(4747, loaded.AUShort);
            Assert.AreEqual("str", loaded.AString);
            Assert.AreEqual(987654L, loaded.AULong);
            Assert.AreEqual(3.14, loaded.ADouble);
        }
    }
}