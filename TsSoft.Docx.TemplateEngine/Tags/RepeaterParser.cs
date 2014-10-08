﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TsSoft.Docx.TemplateEngine.Tags.Processors;

namespace TsSoft.Docx.TemplateEngine.Tags
{
    using System;

    internal class RepeaterParser : GeneralParser
    {
        private const string TagName = "Repeater";
        private const string EndTagName = "EndRepeater";
        private const string ItemsTagName = "Items";
        private const string StartContentTagName = "Content";
        private const string EndContentTagName = "EndContent";
        private const string IndexTag = "ItemIndex";
        private const string ItemTag = "Item";

        public override void Parse(ITagProcessor parentProcessor, XElement startElement)
        {
            this.ValidateStartTag(startElement, TagName);
            var endRepeater = TryGetRequiredTag(startElement, EndTagName);
            var itemsTag = TryGetRequiredTag(startElement, endRepeater, ItemsTagName);

            if (string.IsNullOrEmpty(itemsTag.Value))
            {
                throw new Exception(string.Format(MessageStrings.TagNotFoundOrEmpty, "Items"));
            }

            var startContent = TryGetRequiredTag(startElement, endRepeater, StartContentTagName);
            var endContent = TryGetRequiredTag(startElement, endRepeater, EndContentTagName);

            IEnumerable<XElement> elementsBetween = TraverseUtils.ElementsBetween(startContent, endContent).ToList();
            IEnumerable<RepeaterElement> repeaterElements = elementsBetween.Select(this.MakeRepeaterElement);
            var repeaterTag = new RepeaterTag
                {
                    Source = itemsTag.Value,
                    StartContent = startContent,
                    EndContent = endContent,
                    Content = repeaterElements,
                    StartRepeater = startElement,
                    EndRepeater = endRepeater,
                };

            var repeaterProcessor = new RepeaterProcessor
                {
                    RepeaterTag = repeaterTag,
                };

            this.GoDeeper(repeaterProcessor, elementsBetween);

            parentProcessor.AddProcessor(repeaterProcessor);
        }

        private RepeaterElement MakeRepeaterElement(XElement element)
        {
            var repeaterElement = new RepeaterElement
            {
                Elements = element.Elements().Select(this.MakeRepeaterElement),
                IsIndex = element.IsTag(IndexTag),
                IsItem = element.IsTag(ItemTag),
                XElement = element
            };
            if (repeaterElement.IsItem)
            {
                repeaterElement.Expression = element.GetExpression();
            }
            return repeaterElement;
        }

        private void GoDeeper(ITagProcessor parentProcessor, IEnumerable<XElement> elements)
        {
            foreach (var element in elements)
            {
                if (element.IsSdt())
                {
                    switch (this.GetTagName(element).ToLower())
                    {
                        case "item":
                        case "itemindex":
                            continue;
                        default:
                            this.ParseSdt(parentProcessor, element);
                            break;
                    }
                }
                this.GoDeeper(parentProcessor, element.Elements());
            }
        }
    }
}
