﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Common;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.WebTools.Languages.Shared.ContentTypes;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;

internal class FormattingLanguageServerClient(ILoggerFactory loggerFactory) : ClientNotifierServiceBase
{
    private readonly Dictionary<string, RazorCodeDocument> _documents = [];
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public InitializeResult ServerSettings
        => throw new NotImplementedException();

    public void AddCodeDocument(RazorCodeDocument codeDocument)
    {
        var path = FilePathNormalizer.Normalize(codeDocument.Source.FilePath);
        _documents.TryAdd("/" + path, codeDocument);
    }

    private Task<RazorDocumentFormattingResponse> FormatAsync(DocumentOnTypeFormattingParams @params)
    {
        var generatedHtml = GetGeneratedHtml(@params.TextDocument.Uri);
        var generatedHtmlSource = SourceText.From(generatedHtml, Encoding.UTF8);
        var absoluteIndex = @params.Position.GetRequiredAbsoluteIndex(generatedHtmlSource, logger: null);

        var request = $$"""
            {
                "Options":
                {
                    "UseSpaces": {{(@params.Options.InsertSpaces ? "true" : "false")}},
                    "TabSize": {{@params.Options.TabSize}},
                    "IndentSize": {{@params.Options.TabSize}}
                },
                "Uri": "{{@params.TextDocument.Uri}}",
                "GeneratedChanges": [],
                "OperationType": "FormatOnType",
                "SpanToFormat":
                {
                    "Start": {{absoluteIndex}},
                    "End": {{absoluteIndex}}
                }
            }
            """;

        return CallWebToolsApplyFormattedEditsHandlerAsync(request, @params.TextDocument.Uri, generatedHtml);
    }

    private Task<RazorDocumentFormattingResponse> FormatAsync(DocumentFormattingParams @params)
    {
        var generatedHtml = GetGeneratedHtml(@params.TextDocument.Uri);

        var request = $$"""
            {
                "Options":
                {
                    "UseSpaces": {{(@params.Options.InsertSpaces ? "true" : "false")}},
                    "TabSize": {{@params.Options.TabSize}},
                    "IndentSize": {{@params.Options.TabSize}}
                },
                "Uri": "{{@params.TextDocument.Uri}}",
                "GeneratedChanges": [],
            }
            """;

        return CallWebToolsApplyFormattedEditsHandlerAsync(request, @params.TextDocument.Uri, generatedHtml);
    }

    private string GetGeneratedHtml(Uri uri)
    {
        var codeDocument = _documents[uri.GetAbsoluteOrUNCPath()];
        var generatedHtml = codeDocument.GetHtmlDocument().GeneratedCode;
        return generatedHtml.Replace("\r", "", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private async Task<RazorDocumentFormattingResponse> CallWebToolsApplyFormattedEditsHandlerAsync(string serializedValue, Uri documentUri, string generatedHtml)
    {
        var exportProvider = EditorTestCompositions.Editor.ExportProviderFactory.CreateExportProvider();
        var contentTypeService = exportProvider.GetExportedValue<IContentTypeRegistryService>();

        if (!contentTypeService.ContentTypes.Any(t => t.TypeName == HtmlContentTypeDefinition.HtmlContentType))
        {
            contentTypeService.AddContentType(HtmlContentTypeDefinition.HtmlContentType, new[] { StandardContentTypeNames.Text });
        }

        var textBufferFactoryService = (ITextBufferFactoryService3)exportProvider.GetExportedValue<ITextBufferFactoryService>();
        var bufferManager = WebTools.BufferManager.New(contentTypeService, textBufferFactoryService, []);
        var logger = _loggerFactory.CreateLogger(GetType()).ToRazorLogger();
        var applyFormatEditsHandler = WebTools.ApplyFormatEditsHandler.New(textBufferFactoryService, bufferManager, logger);

        // Make sure the buffer manager knows about the source document
        var textSnapshot = bufferManager.CreateBuffer(
            documentUri: documentUri,
            contentTypeName: HtmlContentTypeDefinition.HtmlContentType,
            initialContent: generatedHtml,
            snapshotVersionFromLSP: 0);

        var requestContext = WebTools.RequestContext.New(textSnapshot);

        var request = WebTools.ApplyFormatEditsParam.DeserializeFrom(serializedValue);
        var response = await applyFormatEditsHandler.HandleRequestAsync(request, requestContext, CancellationToken.None);

        var sourceText = SourceText.From(generatedHtml);

        using var edits = new PooledArrayBuilder<TextEdit>();

        foreach (var textChange in response.TextChanges)
        {
            var startLinePosition = sourceText.Lines.GetLinePosition(textChange.Position);
            var endLinePosition = sourceText.Lines.GetLinePosition(textChange.Position + textChange.Length);

            var edit = new TextEdit()
            {
                Range = new()
                {
                    Start = new(startLinePosition.Line, startLinePosition.Character),
                    End = new(endLinePosition.Line, endLinePosition.Character)
                },
                NewText = textChange.NewText
            };

            edits.Add(edit);
        }

        return new()
        {
            Edits = edits.ToArray()
        };
    }

    public override async Task<TResponse> SendRequestAsync<TParams, TResponse>(string method, TParams @params, CancellationToken cancellationToken)
    {
        if (@params is DocumentFormattingParams formattingParams &&
            string.Equals(method, CustomMessageNames.RazorHtmlFormattingEndpoint, StringComparison.Ordinal))
        {
            var response = await FormatAsync(formattingParams);

            return Convert(response);
        }
        else if (@params is DocumentOnTypeFormattingParams onTypeFormattingParams &&
            string.Equals(method, CustomMessageNames.RazorHtmlOnTypeFormattingEndpoint, StringComparison.Ordinal))
        {
            var response = await FormatAsync(onTypeFormattingParams);

            return Convert(response);
        }

        throw new NotImplementedException();

        static TResponse Convert(RazorDocumentFormattingResponse response)
        {
            return response is TResponse typedResponse
                ? typedResponse
                : throw new InvalidOperationException();
        }
    }

    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }

    public bool TryGetRequest(long id, out string method, out TaskCompletionSource<JToken> pendingTask)
    {
        throw new NotImplementedException();
    }

    public override Task SendNotificationAsync<TParams>(string method, TParams @params, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task SendNotificationAsync(string method, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task OnInitializedAsync(VSInternalClientCapabilities clientCapabilities, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
