using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Microsoft.BizTalk.Component.Interop;
using Microsoft.BizTalk.Message.Interop;
using Microsoft.BizTalk.Streaming;
using Microsoft.BizTalk.XPath;
using IComponent = Microsoft.BizTalk.Component.Interop.IComponent;
using BizTalkComponents.Utils;

namespace Shared.PipelineComponents
{
    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [System.Runtime.InteropServices.Guid("E7F67FC1-FC6C-40B2-BD94-14AA7BD4F9D6")]
    [ComponentCategory(CategoryTypes.CATID_Any)]
    public partial class SuspendBasedOnXPath : IComponent, IBaseComponent, IPersistPropertyBag, IComponentUI
    {
       
        private const string XPathPropertyName = "XPath";
        private const string ExpressionPropertyName = "Expression";

        [DisplayName("XPath")]
        [Description("The XPath to find the value that should be evaluated")]
        [RequiredRuntime]
        public string XPath { get; set; }

        [DisplayName("Expression")]
        [Description("Expression to evaluate on value")]
        [RequiredRuntime]
        public string Expression { get; set; }

        public IBaseMessage Execute(IPipelineContext pContext, IBaseMessage pInMsg)
        {
           
            String value = null;
            bool suspend = false;

            IBaseMessagePart bodyPart = pInMsg.BodyPart;

           

            Stream inboundStream = bodyPart.GetOriginalDataStream();
            VirtualStream virtualStream = new VirtualStream(VirtualStream.MemoryFlag.AutoOverFlowToDisk);
            ReadOnlySeekableStream readOnlySeekableStream = new ReadOnlySeekableStream(inboundStream, virtualStream);

            XmlTextReader xmlTextReader = new XmlTextReader(readOnlySeekableStream);
            XPathCollection xPathCollection = new XPathCollection();
            XPathReader xPathReader = new XPathReader(xmlTextReader, xPathCollection);
            xPathCollection.Add(XPath);

            while (xPathReader.ReadUntilMatch())
            {
                if (xPathReader.Match(0))
                {
                    if (xPathReader.NodeType == XmlNodeType.Attribute)
                    {
                        value = xPathReader.GetAttribute(xPathReader.Name);
                    }
                    else
                    {
                        value = xPathReader.ReadString();
                    }
 
                    break;
                }
            }

           
            suspend = ScriptExpressionHelper.ValidateExpression(value, Expression);

            if (suspend)
            {
              readOnlySeekableStream.Position = 0;
                pContext.ResourceTracker.AddResource(readOnlySeekableStream);
                bodyPart.Data = readOnlySeekableStream;
                pInMsg.Context.Write("SuspendAsNonResumable", "http://schemas.microsoft.com/BizTalk/2003/system-properties", true);
                pInMsg.Context.Write("SuppressRoutingFailureDiagnosticInfo", "http://schemas.microsoft.com/BizTalk/2003/system-properties", true);

                throw new Exception(String.Format("Expression {0} {1} did not evaluate to true", value, Expression));
            }
            else
            {
                pInMsg = null;
            }
           

            return pInMsg;
        }

        public void Load(IPropertyBag propertyBag, int errorLog)
        {
            Expression = PropertyBagHelper.ReadPropertyBag(propertyBag, ExpressionPropertyName, Expression);
            XPath = PropertyBagHelper.ReadPropertyBag(propertyBag, XPathPropertyName, XPath);
         
        }

        public void Save(IPropertyBag propertyBag, bool clearDirty, bool saveAllProperties)
        {
            PropertyBagHelper.WritePropertyBag(propertyBag, ExpressionPropertyName, Expression);
            PropertyBagHelper.WritePropertyBag(propertyBag, XPathPropertyName, XPath);
        }
    }
}
