using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.SemanticKernelPlugin.Internals;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace kernel_memory_with_semantic_kernel.Plugins
{
    internal sealed class ExtendedKernelMemory
    {
        /// <summary>
        /// Default index where to store and retrieve memory from. When null the service
        /// will use a default index for all information.
        /// </summary>
        private readonly string? _defaultIndex;

        private readonly IKernelMemory _memory;

        private readonly Kernel _kernel;

        private readonly string memoryKey = "AskMemoryKey";

        public ExtendedKernelMemory(IKernelMemory memory,
            Kernel kernel)
        {
            _memory = memory;
            _kernel = kernel;
        }

        /// <summary>
        /// Default collection of tags required when retrieving memory (using filters).
        /// </summary>
        private readonly TagCollection? _defaultRetrievalTags;

        [KernelFunction, Description("Use long term memory to answer a question")]
        public async Task<string> AskAsync(
        [ /*SKName(QuestionParam),*/ Description("The question to answer")]
        string question,
        [ /*SKName(IndexParam),*/ Description("Memories index to search for answers"), DefaultValue("")]
        string? index = null,
        [ /*SKName(MinRelevanceParam),*/ Description("Minimum relevance of the sources to consider"), DefaultValue(0d)]
        double minRelevance = 0,
        [ /*SKName(TagsParam),*/ Description("Memories tags to search for information"), DefaultValue(null)]
        TagCollectionWrapper? tags = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
        {
            MemoryAnswer answer = await this._memory.AskAsync(
                question: question,
                index: index ?? this._defaultIndex,
                filter: TagsToMemoryFilter(tags ?? this._defaultRetrievalTags),
                minRelevance: minRelevance,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if(_kernel != null && _kernel.Data.ContainsKey(memoryKey))
            {
                _kernel.Data.Remove(memoryKey);
            }

            _kernel?.Data.Add(memoryKey, answer);

            return answer.Result;
        }

        private static MemoryFilter? TagsToMemoryFilter(TagCollection? tags)
        {
            if (tags == null)
            {
                return null;
            }

            var filters = new MemoryFilter();

            foreach (var tag in tags)
            {
                filters.Add(tag.Key, tag.Value);
            }

            return filters;
        }
    }
}
