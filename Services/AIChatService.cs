using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.RegularExpressions;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Advanced AI Chat Service - Expert E-commerce Assistant
    /// Features: Sentiment Analysis, Spell Correction, Personality Traits, User Memory,
    /// Smart Recommendations, Multi-turn Conversation, Festival Awareness
    /// </summary>
    public class AIChatService : IAIChatService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ICartService _cartService;
        private readonly ILogger<AIChatService> _logger;
        private static readonly Random _random = new();
        private readonly string _baseUrl;

        // Conversation context memory (session-based) with enhanced tracking
        private static readonly Dictionary<int, ConversationContext> _conversationContexts = new();

        // User preference memory (persistent across sessions)
        private static readonly Dictionary<string, UserPreferences> _userPreferences = new();

        // Pending cart actions (for multi-step cart operations)
        private static readonly Dictionary<int, PendingCartAction> _pendingCartActions = new();

        #region Sentiment Analysis Data

        private static readonly Dictionary<string, string[]> _sentimentKeywords = new()
        {
            ["happy"] = new[] { "khushi", "happy", "valo lagche", "moja", "darun", "excellent", "wow", "bah", "চমৎকার", "দারুণ", "মজা", "খুশি", "ভালো লাগছে", "ধন্যবাদ", "thanks", "great", "awesome", "love", "valobashi", "perfect", "best", "😊", "😄", "🥰", "❤️", "👍" },
            ["sad"] = new[] { "sad", "dukhito", "mon kharap", "koshto", "krap", "bad", "দুঃখিত", "কষ্ট", "মন খারাপ", "বিরক্ত", "না পারলাম", "হতাশ", "😢", "😔", "💔" },
            ["angry"] = new[] { "angry", "rag", "birokto", "baje", "kharap", "cheat", "scam", "রাগ", "বিরক্ত", "বাজে", "খারাপ", "প্রতারণা", "ধোঁকা", "মিথ্যা", "lie", "fraud", "😠", "😡", "🤬" },
            ["frustrated"] = new[] { "frustrated", "bujhte parchi na", "ki korbo", "problem", "somossa", "help", "বুঝতে পারছি না", "সমস্যা", "কি করব", "কাজ করছে না", "not working", "error", "issue", "hocche na", "parchi na", "😤", "😫" },
            ["confused"] = new[] { "confused", "bujhi na", "clear na", "কনফিউজড", "বুঝি না", "ক্লিয়ার না", "ki bolte chachen", "mane ki", "কি বলতে চাচ্ছেন", "মানে কি", "🤔", "❓" },
            ["excited"] = new[] { "excited", "can't wait", "onek khushi", "অনেক খুশি", "অপেক্ষায়", "দারুণ", "awesome", "amazing", "🎉", "🤩", "🔥" }
        };

        private static readonly Dictionary<string, string[]> _sentimentResponses = new()
        {
            ["happy"] = new[] {
                "আপনার খুশি দেখে আমারও মন ভালো হয়ে গেল! 🥰",
                "বাহ! আপনার এই পজিটিভ এনার্জি অসাধারণ! ✨",
                "এমনই হাসিখুশি থাকুন সবসময়! 😊"
            },
            ["sad"] = new[] {
                "ওহ না! 😢 মন খারাপের কথা শুনে আমারও মন খারাপ হয়ে গেল। কি হয়েছে বলুন, হয়তো সাহায্য করতে পারব।",
                "আফসোস! চিন্তা করবেন না, সব ঠিক হয়ে যাবে ইনশাআল্লাহ। 🤗 আমি আপনার পাশে আছি।",
                "দুঃখের কথা শুনলাম। 💙 একটু শপিং করলে হয়তো মন ভালো হবে? নাকি কোনো সমস্যায় সাহায্য করতে পারি?"
            },
            ["angry"] = new[] {
                "আপনার রাগ বুঝতে পারছি। 🙏 আসুন শান্ত হয়ে সমস্যাটা জানান, আমি সর্বোচ্চ চেষ্টা করব সমাধান করতে।",
                "রাগ করবেন না প্লিজ! 😔 আপনার সমস্যা সমাধান করতে আমি প্রতিশ্রুতিবদ্ধ।",
                "অসুবিধার জন্য আন্তরিকভাবে দুঃখিত! কি হয়েছে বিস্তারিত বলুন, আমি নিজে দেখছি।"
            },
            ["frustrated"] = new[] {
                "আমি বুঝতে পারছি এটা কতটা বিরক্তিকর হতে পারে। 😔 চলুন ধাপে ধাপে সমাধান করি।",
                "হতাশ হবেন না! আমি আছি না! 💪 সমস্যাটা একটু বুঝিয়ে বলুন।",
                "চিন্তা করবেন না, একসাথে সমাধান বের করব। কি সমস্যা হচ্ছে?"
            },
            ["confused"] = new[] {
                "কোনো সমস্যা নেই! আমি আরো সহজ করে বোঝাচ্ছি। 😊",
                "আচ্ছা, একটু ক্লিয়ার করি। কোন পার্টটা বুঝতে সমস্যা হচ্ছে?",
                "হুম, বুঝলাম। চলুন আবার থেকে শুরু করি সহজভাবে।"
            },
            ["excited"] = new[] {
                "ওয়াও! আপনার এক্সাইটমেন্ট দেখে আমারও এক্সাইটেড লাগছে! 🎉",
                "দারুণ! এই উৎসাহ অসাধারণ! 🚀",
                "বাহ! আপনার এনার্জি সংক্রামক! 🌟"
            }
        };

        #endregion

        #region Spell Correction Data

        private static readonly Dictionary<string, string> _commonMisspellings = new()
        {
            // Common Banglish typos and corrections
            { "prduct", "product" }, { "prdct", "product" }, { "prodct", "product" },
            { "ordr", "order" }, { "oder", "order" }, { "ordeer", "order" },
            { "delivry", "delivery" }, { "dlivery", "delivery" }, { "delivary", "delivery" },
            { "paymnt", "payment" }, { "paymet", "payment" }, { "paiment", "payment" },
            { "prise", "price" }, { "pric", "price" }, { "proce", "price" },
            { "dscount", "discount" }, { "discont", "discount" }, { "discoutn", "discount" },
            { "shoping", "shopping" }, { "shoppin", "shopping" }, { "shpping", "shopping" },
            { "categry", "category" }, { "catagory", "category" }, { "caregory", "category" },
            { "sizee", "size" }, { "saiz", "size" }, { "siez", "size" },
            { "colur", "color" }, { "colr", "color" }, { "colour", "color" },
            { "retrun", "return" }, { "retur", "return" }, { "retrn", "return" },
            { "refnd", "refund" }, { "refud", "refund" }, { "refudn", "refund" },
            { "shiping", "shipping" }, { "shipin", "shipping" }, { "shippng", "shipping" },
            { "acount", "account" }, { "acont", "account" }, { "accont", "account" },
            { "pasword", "password" }, { "passwrd", "password" }, { "passowrd", "password" },
            { "waranty", "warranty" }, { "warrenty", "warranty" }, { "warrnty", "warranty" },
            { "membershp", "membership" }, { "mmbership", "membership" },
            { "premiun", "premium" }, { "primium", "premium" }, { "premim", "premium" },

            // Common Banglish words
            { "kno", "kono" }, { "konoO", "kono" },
            { "amon", "amono" }, { "amn", "amon" },
            { "kemn", "kemon" }, { "kamon", "kemon" },
            { "bolun", "bolun" }, { "bolen", "bolun" },
            { "korbn", "korben" }, { "krben", "korben" },
            { "parbn", "parben" }, { "prben", "parben" },
            { "dkhen", "dekhen" }, { "deken", "dekhen" },
            { "jante", "jante" }, { "jnate", "jante" },
            { "bujhte", "bujhte" }, { "bujte", "bujhte" },
            { "kintte", "kinte" }, { "knte", "kinte" },
            { "lagbbe", "lagbe" }, { "lgbe", "lagbe" },
            { "chaii", "chai" }, { "cai", "chai" },
            { "kortte", "korte" }, { "krte", "korte" },
            { "paoa", "pawa" }, { "paoya", "pawa" },
            { "hbee", "hobe" }, { "hbe", "hobe" },
            { "asee", "ase" }, { "ace", "ase" }, { "ache", "ase" },
            { "naii", "nai" }, { "ni", "nai" },
            { "takai", "taka" }, { "tka", "taka" },
            { "damm", "dam" }, { "daam", "dam" },
            { "kotoo", "koto" }, { "kto", "koto" },
            { "dinee", "dine" }, { "dne", "dine" },
            { "kobee", "kobe" }, { "kbe", "kobe" }
        };

        #endregion

        #region Personality Fillers

        private static readonly string[] _thinkingFillers = new[]
        {
            "আচ্ছা", "হুম", "দেখি", "বুঝলাম", "ওকে", "জি"
        };

        private static readonly string[] _startingFillers = new[]
        {
            "আচ্ছা, ", "হুম, ", "বুঝলাম! ", "ওকে! ", "জি! ", "দেখুন, ", "আসলে, ", "সত্যি বলতে, "
        };

        private static readonly string[] _endingFillers = new[]
        {
            " 😊", " 🤗", " আর কিছু?", " বলুন!", " কি বলেন?", " ঠিক আছে?", ""
        };

        private static readonly string[] _encouragingPhrases = new[]
        {
            "আপনি ঠিক পথে আছেন!",
            "দারুণ প্রশ্ন!",
            "ভালো যে জিজ্ঞেস করলেন!",
            "এটা জানা দরকার!",
            "স্মার্ট চয়েস!"
        };

        #endregion

        #region Bangladesh Festival Data

        private static readonly Dictionary<string, (DateTime Start, DateTime End, string Greeting)> _festivals2024 = new()
        {
            ["eid_ul_fitr"] = (new DateTime(2024, 4, 10), new DateTime(2024, 4, 12), "ঈদ মুবারক! 🌙✨ সবাইকে ঈদের শুভেচ্ছা!"),
            ["eid_ul_adha"] = (new DateTime(2024, 6, 17), new DateTime(2024, 6, 19), "ঈদ মুবারক! 🐄🌙 কুরবানির ঈদের শুভেচ্ছা!"),
            ["pohela_boishakh"] = (new DateTime(2024, 4, 14), new DateTime(2024, 4, 14), "শুভ নববর্ষ! 🎉 বাংলা নববর্ষের শুভেচ্ছা!"),
            ["victory_day"] = (new DateTime(2024, 12, 16), new DateTime(2024, 12, 16), "বিজয় দিবসের শুভেচ্ছা! 🇧🇩 জয় বাংলা!"),
            ["independence_day"] = (new DateTime(2024, 3, 26), new DateTime(2024, 3, 26), "স্বাধীনতা দিবসের শুভেচ্ছা! 🇧🇩"),
            ["valentines_day"] = (new DateTime(2024, 2, 14), new DateTime(2024, 2, 14), "হ্যাপি ভ্যালেন্টাইনস ডে! ❤️ প্রিয়জনকে গিফট দিন!"),
            ["mothers_day"] = (new DateTime(2024, 5, 12), new DateTime(2024, 5, 12), "শুভ মা দিবস! 💐 মাকে ভালোবাসুন!"),
            ["fathers_day"] = (new DateTime(2024, 6, 16), new DateTime(2024, 6, 16), "শুভ বাবা দিবস! 👔 বাবাকে ভালোবাসুন!"),
            ["durga_puja"] = (new DateTime(2024, 10, 9), new DateTime(2024, 10, 13), "শুভ দুর্গাপূজা! 🙏 শারদীয়া শুভেচ্ছা!")
        };

        private static readonly Dictionary<int, string> _seasonalMessages = new()
        {
            [1] = "🥶 শীতকাল চলছে! গরম কাপড় দেখতে 'winter collection' লিখুন!",
            [2] = "🌸 বসন্ত আসছে! নতুন ফ্যাশন কালেকশন দেখুন!",
            [3] = "☀️ গরম পড়ছে! সামার কালেকশন দেখুন!",
            [4] = "🌞 গ্রীষ্মকাল! হালকা পোশাক ও কুলিং প্রোডাক্ট দেখুন!",
            [5] = "⛈️ বর্ষা আসছে! রেইনকোট ও ছাতা দেখুন!",
            [6] = "☔ বর্ষাকাল! ওয়াটারপ্রুফ আইটেম দেখুন!",
            [7] = "🌧️ বর্ষা মৌসুম! ইনডোর আইটেম দেখুন!",
            [8] = "🌧️ বর্ষা শেষের দিকে! শরৎ কালেকশন আসছে!",
            [9] = "🍂 শরৎকাল! পূজার শপিং করুন!",
            [10] = "🪔 শারদীয়া সিজন! ফেস্টিভ কালেকশন দেখুন!",
            [11] = "❄️ শীত আসছে! উইন্টার কালেকশন দেখুন!",
            [12] = "🎄 বিজয়ের মাস! উইন্টার সেল চলছে!"
        };

        #endregion

        public AIChatService(
            ApplicationDbContext db,
            ISiteSettingsService siteSettingsService,
            ICartService cartService,
            ILogger<AIChatService> logger,
            IConfiguration configuration)
        {
            _db = db;
            _siteSettingsService = siteSettingsService;
            _cartService = cartService;
            _logger = logger;
            _baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:5005";
        }

        #region Public Methods

        public async Task<string> GetGreetingMessageAsync(string visitorName, string? initialQuery)
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            var siteName = siteSettings?.SiteName ?? "Bangaliyana";
            var hour = DateTime.Now.Hour;

            var timeGreeting = hour < 12 ? "সুপ্রভাত" : (hour < 17 ? "শুভ দুপুর" : (hour < 20 ? "শুভ সন্ধ্যা" : "শুভ রাত্রি"));

            // Get some dynamic stats for greeting
            var productCount = await _db.Products.CountAsync(p => p.Status == ProductStatus.Active);
            var categoryCount = await _db.Categories.CountAsync(c => c.IsActive);

            var greetings = new[]
            {
                $"আসসালামু আলাইকুম {visitorName}! {timeGreeting}! 😊\n\n{siteName} এ স্বাগতম! আমি আপনার AI সহকারী - ২৪/৭ আপনার সেবায় হাজির!\n\n📦 {productCount}+ পণ্য | 📁 {categoryCount}+ ক্যাটাগরি\n\nবলুন, আজ কিভাবে সাহায্য করতে পারি?",
                $"{timeGreeting} {visitorName}! 🌟\n\n{siteName} এ আপনাকে স্বাগতম। আমি আপনার বাংলালিয়ানা বন্ধু! পণ্য খোঁজা থেকে অর্ডার ট্র্যাকিং - সবকিছুতে সাহায্য করতে পারি!\n\nকি দরকার বলুন?"
            };

            var greeting = greetings[_random.Next(greetings.Length)];

            if (!string.IsNullOrEmpty(initialQuery))
            {
                greeting += $"\n\n💭 আপনার প্রশ্ন \"{initialQuery}\" নিয়ে এখনই সাহায্য করছি...";
            }

            return greeting;
        }

        public async Task<AIChatResponse> GetResponseAsync(string userMessage, int sessionId, string? userId = null)
        {
            try
            {
                var originalMessage = userMessage;

                // Step 1: Spell correction
                var correctedMessage = CorrectSpelling(userMessage);
                var normalizedMessage = NormalizeBanglish(correctedMessage.ToLower().Trim());

                _logger.LogInformation("Processing: {Original} -> Corrected: {Corrected} -> Normalized: {Normalized}",
                    originalMessage, correctedMessage, normalizedMessage);

                // Step 2: Get conversation context
                var context = GetOrCreateContext(sessionId);
                context.LastMessage = originalMessage;
                context.MessageCount++;
                context.LastActivityAt = DateTime.UtcNow;

                // Step 3: Detect sentiment for empathetic responses
                var (sentiment, sentimentConfidence) = DetectSentiment(originalMessage);
                context.DetectedSentiment = sentiment;
                _logger.LogInformation("Sentiment: {Sentiment} (Confidence: {Confidence})", sentiment, sentimentConfidence);

                // Step 4: Check for follow-up questions
                context.IsFollowUp = IsFollowUpQuestion(originalMessage, context);
                if (context.IsFollowUp)
                {
                    _logger.LogInformation("Detected follow-up question, using context from previous message");
                }

                // Step 5: Update user preferences
                UpdateUserPreferences(userId, intent: normalizedMessage.Split(' ').FirstOrDefault());

                var response = new AIChatResponse();
                var sentimentPrefix = sentimentConfidence > 0.1 ? GetSentimentAwarePrefix(sentiment) : "";

                // Step 6: First check for order number in message
                var orderNumber = ExtractOrderNumber(originalMessage);
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    context.LastOrderNumber = orderNumber;
                    response = await HandleOrderTrackingAsync(orderNumber, userId);
                    response.Message = sentimentPrefix + response.Message;
                    return response;
                }

                // Step 7: Check for product name/search in message
                var productSearch = ExtractProductQuery(normalizedMessage, originalMessage);

                // If follow-up, use previous product query
                if (context.IsFollowUp && string.IsNullOrEmpty(productSearch))
                {
                    productSearch = HandleFollowUp(context, productSearch ?? "");
                }

                // Step 8: Detect intent using fuzzy matching
                var (intent, confidence, matchedKeyword) = DetectIntentFuzzy(normalizedMessage);
                _logger.LogInformation("Intent: {Intent}, Confidence: {Confidence}, Matched: {Matched}", intent, confidence, matchedKeyword);

                // If low confidence and we have product-like text, treat as product search
                if (confidence < 0.3 && !string.IsNullOrEmpty(productSearch))
                {
                    response = await HandleProductSearchAsync(productSearch, originalMessage);
                    context.LastProductQuery = productSearch;
                    response.Message = sentimentPrefix + AddPersonalityToResponse(response.Message, context);
                    return response;
                }

                // Step 9: Check FAQ database for matching questions
                if (confidence < 0.5)
                {
                    var faqMatch = await FindMatchingFAQAsync(normalizedMessage, originalMessage);
                    if (faqMatch != null)
                    {
                        var faqResponse = $"📋 **FAQ থেকে উত্তর:**\n\n{faqMatch.Answer}\n\n❓ এই উত্তর কি সাহায্য করেছে? আরো প্রশ্ন থাকলে বলুন!";
                        return new AIChatResponse
                        {
                            Message = sentimentPrefix + AddPersonalityToResponse(faqResponse, context),
                            FAQSuggestions = new List<AIFAQSuggestion> { new() { Id = faqMatch.Id, Question = faqMatch.Question, Answer = faqMatch.Answer } }
                        };
                    }
                }

                // Step 10: Handle based on detected intent
                response = intent switch
                {
                    // Social & Conversational
                    "greeting" => new AIChatResponse { Message = await HandleGreetingWithExtrasAsync(userId) },
                    "how_are_you" => new AIChatResponse { Message = HandleHowAreYou() },
                    "who_are_you" => new AIChatResponse { Message = await HandleWhoAreYouAsync() },
                    "what_can_you_do" => new AIChatResponse { Message = await HandleWhatCanYouDoAsync() },
                    "creator" => new AIChatResponse { Message = await HandleCreatorQueryAsync() },
                    "joke" => new AIChatResponse { Message = HandleJoke() },
                    "compliment" => new AIChatResponse { Message = HandleCompliment() },
                    "feeling_good" => new AIChatResponse { Message = HandleFeelingGood() },
                    "feeling_bad" => new AIChatResponse { Message = HandleFeelingBad() },
                    "love" => new AIChatResponse { Message = HandleLove() },
                    "age" => new AIChatResponse { Message = HandleAge() },
                    "thanks" => new AIChatResponse { Message = HandleThanks() },
                    "bye" => new AIChatResponse { Message = HandleBye() },
                    "yes" => new AIChatResponse { Message = HandleYes(context) },
                    "no" => new AIChatResponse { Message = HandleNo() },
                    "time" => new AIChatResponse { Message = HandleTime() },
                    "weather" => new AIChatResponse { Message = HandleWeather() },

                    // E-commerce
                    "order_status" => await HandleOrderQueryAsync(normalizedMessage, originalMessage, userId, context),
                    "product_search" => await HandleProductSearchWithMemoryAsync(productSearch ?? normalizedMessage, originalMessage, context, userId),
                    "payment" => await HandlePaymentQueryAsync(),
                    "return_refund" => await HandleReturnRefundQueryAsync(),
                    "shipping" => await HandleShippingQueryAsync(),
                    "account" => new AIChatResponse { Message = HandleAccountQuery() },
                    "contact" => await HandleContactQueryAsync(),
                    "help" => await HandleHelpAsync(),
                    "faq_category" => await HandleFaqCategoryIntentAsync(normalizedMessage, originalMessage),
                    "discount" => await HandleDiscountQueryAsync(userId),
                    "stock" => await HandleStockQueryAsync(productSearch ?? normalizedMessage, originalMessage, context),
                    "size" => await HandleSizeQueryAsync(productSearch, context),
                    "color" => await HandleColorQueryAsync(productSearch, context),
                    "warranty" => new AIChatResponse { Message = HandleWarrantyQuery() },
                    "cod" => await HandleCODQueryAsync(),
                    "bulk_order" => new AIChatResponse { Message = HandleBulkOrderQuery() },
                    "gift" => new AIChatResponse { Message = HandleGiftQuery() },
                    "new_arrival" => await HandleNewArrivalQueryAsync(),
                    "best_seller" => await HandleBestSellerQueryAsync(),
                    "review" => await HandleReviewQueryAsync(productSearch, context),
                    "compare" => new AIChatResponse { Message = HandleCompareQuery() },
                    "category" => await HandleCategoryQueryAsync(normalizedMessage, context),
                    "navigation" => HandleNavigationQuery(),
                    "express" => await HandleExpressDeliveryQueryAsync(),
                    "membership" => await HandleMembershipQueryAsync(),
                    "seller" => await HandleSellerQueryAsync(normalizedMessage),
                    "complaint" => new AIChatResponse { Message = HandleComplaintQuery() },
                    "price" => await HandlePriceQueryAsync(productSearch ?? normalizedMessage, originalMessage, context),
                    "recommendation" => await HandleRecommendationQueryAsync(userId, context),
                    "human_request" => await HandleHumanAgentRequestAsync(context),
                    "add_to_cart" => await HandleAddToCartIntentAsync(normalizedMessage, originalMessage, context, userId),
                    "view_cart" => await HandleViewCartAsync(userId),

                    // ============ NEW ADVANCED FEATURES ============

                    // Order Tracking - Enhanced
                    "track_order" => await HandleTrackOrderIntentAsync(normalizedMessage, originalMessage, context, userId),
                    "my_orders" => await HandleMyOrdersIntentAsync(context, userId),

                    // Wishlist Management
                    "wishlist_add" => await HandleWishlistAddIntentAsync(normalizedMessage, originalMessage, context, userId),
                    "wishlist_view" => await HandleWishlistViewIntentAsync(context, userId),
                    "wishlist_remove" => await HandleWishlistRemoveIntentAsync(normalizedMessage, originalMessage, context, userId),
                    "wishlist_to_cart" => await HandleWishlistToCartIntentAsync(normalizedMessage, originalMessage, context, userId),

                    // Coupon & Discount
                    "find_coupon" => await HandleFindCouponIntentAsync(context, userId),
                    "apply_coupon" => await HandleApplyCouponIntentAsync(normalizedMessage, originalMessage, context, userId),

                    // Return & Refund
                    "return_request" => await HandleReturnRequestIntentAsync(normalizedMessage, originalMessage, context, userId),
                    "refund_status" => await HandleRefundStatusIntentAsync(normalizedMessage, originalMessage, context, userId),
                    "return_policy" => await GetReturnPolicyAsync(),

                    // Product Comparison
                    "compare_products" => await HandleCompareProductsIntentAsync(normalizedMessage, originalMessage, context),

                    // Product Q&A
                    "product_question" => await HandleProductQuestionIntentAsync(normalizedMessage, originalMessage, context),

                    // Reorder & Suggestions
                    "reorder" => await HandleReorderIntentAsync(userId),
                    "frequently_bought" => await HandleFrequentlyBoughtIntentAsync(normalizedMessage, originalMessage, context),

                    _ => await HandleUnknownIntentAsync(normalizedMessage, originalMessage, context)
                };

                // Step 11: Add sentiment-aware prefix for strong emotions
                if (sentimentConfidence > 0.15 && sentiment != "neutral" && sentiment != "happy")
                {
                    response.Message = sentimentPrefix + response.Message;
                }

                // Step 12: Add personality to response
                response.Message = AddPersonalityToResponse(response.Message, context);

                // Step 13: Save context
                context.LastIntent = intent;
                if (!string.IsNullOrEmpty(productSearch))
                {
                    context.LastProductQuery = productSearch;
                    context.MentionedProducts.Add(productSearch);
                }

                // Update LastMentionedProductId from response if products were found
                if (response.ProductSuggestions?.Any() == true)
                {
                    context.LastMentionedProductId = response.ProductSuggestions.First().Id;
                }

                context.ConversationTopics.Add(intent);
                _conversationContexts[sessionId] = context;

                // Step 14: Update user preferences based on intent
                UpdateUserPreferences(userId, intent: intent);

                // Step 15: Enhance response with quick replies, cross-sell, urgency, etc.
                response = await EnhanceResponseAsync(response, intent, context, userId);

                // Step 16: Set confidence score
                response.ConfidenceScore = confidence;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response for session {SessionId}", sessionId);
                return new AIChatResponse
                {
                    Message = "দুঃখিত! একটু টেকনিক্যাল সমস্যা হয়ে গেছে। 😅 আবার চেষ্টা করুন অথবা আমাদের হেল্পলাইনে কল করুন।",
                    IsSuccessful = false
                };
            }
        }

        // Enhanced greeting with festival awareness and personalization
        private async Task<string> HandleGreetingWithExtrasAsync(string? userId)
        {
            var baseGreeting = await HandleGreetingAsync();

            // Add festival greeting if applicable
            var festivalGreeting = GetFestivalGreeting();
            if (!string.IsNullOrEmpty(festivalGreeting))
            {
                baseGreeting = festivalGreeting + "\n\n" + baseGreeting;
            }

            // Add personalized greeting for returning users
            var personalizedGreeting = await GetPersonalizedGreetingAsync(userId);
            if (!string.IsNullOrEmpty(personalizedGreeting))
            {
                baseGreeting = personalizedGreeting + "\n\n" + baseGreeting;
            }

            // Add seasonal suggestion occasionally
            if (_random.NextDouble() > 0.7)
            {
                baseGreeting += "\n\n" + GetSeasonalSuggestion();
            }

            return baseGreeting;
        }

        // Product search with memory and recommendations
        private async Task<AIChatResponse> HandleProductSearchWithMemoryAsync(string query, string originalMessage, ConversationContext context, string? userId)
        {
            var response = await HandleProductSearchAsync(query, originalMessage);

            // Store product query in context
            context.LastProductQuery = query;

            // If search returned results, update context for ALL users (not just logged in)
            if (response.ProductSuggestions?.Any() == true)
            {
                var firstProduct = response.ProductSuggestions.First();
                var product = await _db.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == firstProduct.Id);

                if (product?.Category != null)
                {
                    // Update context LastCategory for ALL users (including guests)
                    context.LastCategory = product.Category.Name;

                    // Update user preferences only for logged-in users
                    if (!string.IsNullOrEmpty(userId))
                    {
                        UpdateUserPreferences(userId, category: product.Category.Name, productName: product.Name);
                    }
                }
            }

            return response;
        }

        // Smart recommendation handler
        private async Task<AIChatResponse> HandleRecommendationQueryAsync(string? userId, ConversationContext context)
        {
            var recommendations = await GetSmartRecommendationsAsync(userId, context);

            if (!recommendations.Any())
            {
                return new AIChatResponse
                {
                    Message = "🛍️ এই মুহূর্তে বিশেষ কোনো রেকমেন্ডেশন নেই। আপনার পছন্দ জানান, আমি সেরা পণ্য দেখাব!"
                };
            }

            var productList = string.Join("\n\n", recommendations.Select(p =>
            {
                var priceText = p.DiscountPrice.HasValue
                    ? $"~~৳{p.Price:N0}~~ **৳{p.DiscountPrice:N0}**"
                    : $"**৳{p.Price:N0}**";
                return $"⭐ **{p.Name}**\n   {priceText}";
            }));

            var introText = !string.IsNullOrEmpty(userId)
                ? "🎯 আপনার জন্য পার্সোনালাইজড রেকমেন্ডেশন:"
                : "🔥 আমাদের ট্রেন্ডিং পণ্য:";

            return new AIChatResponse
            {
                Message = $"{introText}\n\n{productList}\n\n💡 কোনটা দেখবেন? নাম লিখুন!",
                ProductSuggestions = recommendations
            };
        }

        #endregion

        #region Banglish Processing & Fuzzy Matching

        /// <summary>
        /// Normalizes Banglish text by converting phonetic variations to standard form
        /// </summary>
        private string NormalizeBanglish(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var result = text.ToLower().Trim();

            // Common Banglish phonetic normalization
            var replacements = new Dictionary<string, string>
            {
                // Vowels
                { "aa", "a" }, { "ee", "i" }, { "oo", "u" }, { "ou", "o" },

                // Common word variations
                { "ache", "ase" }, { "achen", "asen" }, { "hobe", "hbe" },
                { "korbo", "krbo" }, { "korte", "krte" }, { "korben", "krben" },
                { "kemon", "kmon" }, { "kemne", "kmne" }, { "kivabe", "kvabe" },
                { "bolun", "blun" }, { "bolben", "blben" }, { "bolen", "blen" },
                { "dekhen", "dkhen" }, { "dekhun", "dkhun" }, { "dekhao", "dkhao" },
                { "kinbo", "knbo" }, { "kinben", "knben" }, { "kinte", "knte" },
                { "pabo", "pbo" }, { "paben", "pben" }, { "paoa", "poa" },
                { "chai", "cai" }, { "chaina", "caina" }, { "chaile", "caile" },
                { "lagbe", "lgbe" }, { "lagbena", "lgbena" }, { "lage", "lge" },
                { "diben", "dben" }, { "dibo", "dbo" }, { "din", "dn" },
                { "jante", "jnte" }, { "jani", "jni" }, { "janao", "jnao" },
                { "bujhi", "bjhi" }, { "bujhte", "bjhte" }, { "bujhlam", "bjhlam" },
                { "order", "ordr" }, { "product", "prdct" }, { "price", "prc" },
                { "delivery", "dlvry" }, { "payment", "pymnt" }, { "return", "rtrn" },
                { "refund", "rfnd" }, { "discount", "dscnt" }, { "coupon", "cpn" },

                // Remove repeated characters (but keep meaningful ones)
                { "aaa", "a" }, { "ooo", "o" }, { "eee", "e" }, { "iii", "i" },
            };

            foreach (var replacement in replacements)
            {
                result = result.Replace(replacement.Key, replacement.Value);
            }

            return result;
        }

        /// <summary>
        /// Calculates similarity between two strings using Levenshtein distance
        /// </summary>
        private double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0;
            if (source == target) return 1;

            int sourceLength = source.Length;
            int targetLength = target.Length;

            // Exact substring match
            if (source.Contains(target) || target.Contains(source))
            {
                return 0.8 + (0.2 * Math.Min(source.Length, target.Length) / Math.Max(source.Length, target.Length));
            }

            // Levenshtein distance
            int[,] distance = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; distance[i, 0] = i++) { }
            for (int j = 0; j <= targetLength; distance[0, j] = j++) { }

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            int levenshteinDistance = distance[sourceLength, targetLength];
            return 1.0 - ((double)levenshteinDistance / Math.Max(sourceLength, targetLength));
        }

        /// <summary>
        /// Intent detection with fuzzy matching support
        /// </summary>
        private (string intent, double confidence, string matchedKeyword) DetectIntentFuzzy(string message)
        {
            var intentKeywords = GetIntentKeywords();
            string bestIntent = "unknown";
            double bestScore = 0;
            string bestKeyword = "";

            foreach (var intent in intentKeywords)
            {
                foreach (var keyword in intent.Value)
                {
                    // Exact match (highest priority)
                    if (message.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        double score = 0.9 + (keyword.Length * 0.01); // Longer keywords = higher score
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestIntent = intent.Key;
                            bestKeyword = keyword;
                        }
                    }
                    else
                    {
                        // Fuzzy match
                        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var word in words)
                        {
                            double similarity = CalculateSimilarity(NormalizeBanglish(word), NormalizeBanglish(keyword));
                            if (similarity > 0.7 && similarity > bestScore)
                            {
                                bestScore = similarity * 0.85; // Slightly lower than exact match
                                bestIntent = intent.Key;
                                bestKeyword = keyword;
                            }
                        }
                    }
                }
            }

            return (bestIntent, bestScore, bestKeyword);
        }

        private Dictionary<string, string[]> GetIntentKeywords()
        {
            return new Dictionary<string, string[]>
            {
                // Greetings & Social
                ["greeting"] = new[] { "hi", "hello", "hey", "assalamu", "salam", "হাই", "হ্যালো", "আসসালামু", "সালাম", "নমস্কার", "good morning", "good afternoon", "good evening", "সুপ্রভাত", "শুভ সন্ধ্যা", "শুভ রাত্রি", "হাই হাই", "হাইয়া", "hiya", "hii", "helloo" },
                ["how_are_you"] = new[] { "how are you", "how r u", "kemon acho", "kemon achen", "কেমন আছ", "কেমন আছো", "কেমন আছেন", "ki khobor", "ki obostha", "কি খবর", "কি অবস্থা", "ভালো আছো", "সব ঠিক", "whats up", "wassup", "sup", "kmn acho", "kmn aso", "kemn aco", "ki kbr", "কিরে" },
                ["who_are_you"] = new[] { "who are you", "what are you", "your name", "tomar nam", "তোমার নাম", "আপনার নাম", "তুমি কে", "আপনি কে", "কে তুমি", "নাম কি", "introduce yourself", "পরিচয়", "tumi ke", "apni ke", "nam ki", "kon ai" },
                ["what_can_you_do"] = new[] { "what can you do", "ki korte paro", "কি পারো", "কি করতে পার", "তুমি কি পারো", "আপনি কি পারেন", "সাহায্য করতে পারবে", "help me with", "ki ki paro", "capabilities", "ki krte paro", "ki paro tumi", "help korba" },
                ["creator"] = new[] { "who made you", "who created", "ke baniyeche", "কে বানিয়েছে", "কে তৈরি করেছে", "তোমাকে কে", "আপনাকে কে", "developer", "made by", "ke bnayese", "ke toiri", "tomake ke" },
                ["joke"] = new[] { "joke", "funny", "make me laugh", "হাসাও", "জোক", "মজার কিছু", "হাসির", "মজা করো", "hasao", "moja", "comedy", "hasir golpo", "mojar kisu", "tell joke", "ekta joke" },
                ["compliment"] = new[] { "you are good", "you are great", "nice", "awesome", "amazing", "ভালো", "অসাধারণ", "চমৎকার", "সুন্দর", "বাহ", "wow", "darun", "দারুণ", "excellent", "best", "osadaron", "chomtkar", "sundor", "valo lage" },
                ["feeling_good"] = new[] { "i am good", "i am fine", "i'm good", "i'm fine", "আমি ভালো", "ভালো আছি", "মজায় আছি", "সব ঠিক আছে", "ami valo", "feeling great", "valo asi", "mst aci", "vlo aci" },
                ["feeling_bad"] = new[] { "i am sad", "i'm sad", "not good", "feeling bad", "upset", "মন খারাপ", "দুঃখিত", "কষ্ট", "বিরক্ত", "রাগ", "mon kharap", "problem ache", "dukkho", "mon valo na", "krap lagse" },
                ["love"] = new[] { "i love you", "love you", "ভালোবাসি", "তোমাকে ভালোবাসি", "লাভ ইউ", "valobashi", "like you", "vlobashi", "tomake valo", "toke valo" },
                ["age"] = new[] { "how old", "your age", "বয়স কত", "তোমার বয়স", "কত বছর", "boyos", "age koto", "boyosh kto", "koto boyos" },
                ["thanks"] = new[] { "thank", "thanks", "ধন্যবাদ", "থ্যাংকস", "শুকরিয়া", "dhonnobad", "ty", "tysm", "appreciated", "dhnyobad", "thnx", "thx", "thanks a lot" },
                ["bye"] = new[] { "bye", "goodbye", "বাই", "বিদায়", "আল্লাহ হাফেজ", "খোদা হাফেজ", "টাটা", "tata", "see you", "gtg", "gotta go", "bai", "biday", "allah hafez", "khoda hafez" },
                ["yes"] = new[] { "yes", "yeah", "yep", "ok", "okay", "হ্যাঁ", "জি", "ঠিক আছে", "অবশ্যই", "sure", "definitely", "han", "ji", "thik ase", "onek", "hmm" },
                ["no"] = new[] { "no", "nope", "না", "নাহ", "চাই না", "লাগবে না", "nah", "na lagbe", "cai na", "lgbe na", "dorkar nai" },
                ["time"] = new[] { "what time", "কয়টা বাজে", "সময় কত", "টাইম কত", "time", "koyta baje", "somoy kto" },
                ["weather"] = new[] { "weather", "আবহাওয়া", "বৃষ্টি", "রোদ", "গরম", "ঠান্ডা", "abohawa", "bristi", "rod", "gorom", "thanda" },

                // E-commerce Core - Enhanced with more Banglish variations
                ["order_status"] = new[] { "order", "status", "tracking", "where is my", "shipped", "অর্ডার", "ট্র্যাকিং", "কোথায় আমার", "order ki", "delivery kobe", "kobe pabo", "parcel", "courier", "amar order", "order ta", "order number", "order status", "ordr", "kothay order", "kobe diba", "kobe asbe", "parcel koi", "koriar", "ki obostha", "order er obostha", "order dekhao", "track koro", "order ki holo", "order ki hoilo" },
                ["product_search"] = new[] { "product", "item", "buy", "কিনতে", "পণ্য", "দাম", "প্রোডাক্ট", "show me", "dekhao", "kinbo", "chai", "চাই", "লাগবে", "kinben", "dkhan", "dekhte", "prdct", "kinte", "lagbe", "ki ase", "ki ki ase", "dam kto", "daam", "দেখাও", "দেখান", "দেখি", "দেখুন", "পণ্য দেখাও", "সব পণ্য", "আরো দেখাও", "আরও দেখাও", "পণ্য খোঁজো", "ponno khojo", "কিছু কিনতে চাই", "kichu kinte chai", "দেখাতে পারেন", "dekhate paren", "এমন কিছু", "eman kichu", "এধরনের", "etharonr", "পাওয়া যাবে", "pawa jabe", "আছে কি সেরকম", "ase ki serokm", "খুঁজছি", "khujchi", "কোথায় পাব", "kothay pabo", "search", "সার্চ" },
                ["price"] = new[] { "price", "cost", "dam", "koto dam", "দাম", "কত দাম", "দাম কত", "কত টাকা", "taka koto", "price ki", "dam ki", "kto taka", "tka kto", "কত পড়বে", "koto porbe" },
                ["payment"] = new[] { "payment", "pay", "bkash", "nagad", "card", "পেমেন্ট", "টাকা", "বিকাশ", "নগদ", "rocket", "upay", "ssl", "visa", "mastercard", "taka dibo", "kivabe dibo", "pymnt", "tka", "bikash", "nogod", "roket", "taka ki", "payment ki", "ki diye", "pay korbo", "পেমেন্ট করুন", "পেমেন্ট করব", "টাকা পাঠাব", "টাকা দিব", "পে করব", "কিভাবে পে করব", "পেমেন্ট অপশন", "পেমেন্ট মেথড" },
                ["return_refund"] = new[] { "return", "refund", "exchange", "cancel", "রিটার্ন", "রিফান্ড", "ফেরত", "বাতিল", "এক্সচেঞ্জ", "change korbo", "ferot", "money back", "rtrn", "rfnd", "cancel korbo", "batil", "taka ferot", "poysa ferot", "return policy", "ferot dite", "ফেরত চাই", "পাল্টাতে চাই", "ভুল পণ্য", "টাকা ফেরত চাই", "রিটার্ন করতে চাই", "চেঞ্জ করতে চাই", "ক্যান্সেল করতে চাই", "ফেরত দিতে চাই" },
                ["shipping"] = new[] { "shipping", "delivery", "charge", "free delivery", "ডেলিভারি চার্জ", "শিপিং", "ফ্রি ডেলিভারি", "delivery charge koto", "koto din lage", "dlvry", "shipping charge", "kobe pabo", "kodin lage", "dlvry chrg", "delivery cost", "shipping cost", "pouche diba", "dite prba", "কতদিন লাগবে", "কবে দেবে", "কবে দিবে", "ফ্রি ডেলিভারি আছে", "দ্রুত পৌঁছাবে", "কবে পৌঁছাবে", "ডেলিভারি ফ্রি", "ডেলিভারি খরচ কত" },
                ["account"] = new[] { "account", "login", "password", "register", "sign up", "একাউন্ট", "লগইন", "পাসওয়ার্ড", "রেজিস্টার", "profile", "sign in", "aknt", "pasword", "pasowrd", "account khulte", "login korte" },
                ["contact"] = new[] { "contact", "phone", "email", "address", "যোগাযোগ", "ফোন", "ইমেইল", "ঠিকানা", "helpline", "call", "number", "fone", "email ki", "address ki", "thikana", "call dibo", "fon", "nmber" },
                ["help"] = new[] { "help", "support", "problem", "issue", "সাহায্য", "সমস্যা", "হেল্প", "সাপোর্ট", "ki korbo", "bujhte parchi na", "help koro", "sahajjo", "shomossa", "ki krbo", "bujhi na", "bujhte parci na", "কি করতে হবে", "কিভাবে", "এটা কিভাবে কাজ করে", "বুঝতে পারছি না", "কিভাবে করব", "সাহায্য করুন", "সাহায্য লাগবে", "হেল্প দরকার", "সাপোর্ট দরকার", "support dorkar", "সাপোর্ট চাই" },

                // Advanced E-commerce
                ["discount"] = new[] { "discount", "offer", "coupon", "promo", "code", "ছাড়", "অফার", "কুপন", "ডিসকাউন্ট", "sale", "deal", "voucher", "কত ছাড়", "offer ache", "discount ache", "dscnt", "ofr", "cpn", "koto chhad", "cad ase", "discount ase ki", "offer ta" },
                ["stock"] = new[] { "stock", "available", "আছে কি", "পাওয়া যাবে", "in stock", "out of stock", "স্টক", "availability", "ache ki", "pawa jabe", "stock ase", "stock ache", "ase ki", "paoa jabe", "poa jbe", "available ase" },
                ["size"] = new[] { "size", "সাইজ", "measurement", "মাপ", "fitting", "ফিটিং", "small", "medium", "large", "xl", "xxl", "size chart", "ki size", "amar size", "sz", "map", "fiiting", "kon size", "saiz", "mape koto", "সাইজ গাইড", "size guide", "সাইজ চার্ট", "size chart দেখাও", "সাইজ চার্ট দেখাও" },
                ["color"] = new[] { "color", "colour", "রঙ", "কালার", "কোন রঙে", "available colors", "ki color", "rong", "rang", "colour ki", "kon colour", "ki rongge", "ki ronge" },
                ["warranty"] = new[] { "warranty", "guarantee", "ওয়ারেন্টি", "গ্যারান্টি", "replacement", "damage", "broken", "নষ্ট", "warrenty", "garantee", "granti", "nosto hole", "venge gele" },
                ["cod"] = new[] { "cod", "cash on delivery", "ক্যাশ অন ডেলিভারি", "হাতে নিয়ে", "hand e niye", "age taka dibo na", "পরে দিব", "cash delivery", "hate niye", "pore dibo", "age tka na" },
                ["bulk_order"] = new[] { "bulk", "wholesale", "পাইকারি", "বাল্ক", "large quantity", "অনেকগুলো", "onek gulo", "100 ta", "50 ta", "onekgulo", "paikari", "wholesale price", "gulo kinbo" },
                ["gift"] = new[] { "gift", "উপহার", "gift wrap", "গিফট", "surprise", "for someone", "present", "birthday", "anniversary", "upohar", "bd gift", "jonmodin" },
                ["new_arrival"] = new[] { "new", "latest", "নতুন", "নতুন এসেছে", "new arrival", "just arrived", "notun", "new collection", "notun esece", "notun ase", "latest product", "naya", "fresh" },
                ["best_seller"] = new[] { "best seller", "popular", "বেস্ট সেলার", "জনপ্রিয়", "trending", "most sold", "top", "best product", "কোনটা ভালো", "best", "bst", "popular product", "jonopriyo", "top selling" },
                ["review"] = new[] { "review", "rating", "রিভিউ", "রেটিং", "feedback", "কেমন", "মান", "quality", "original", "নকল", "আসল", "rvw", "orijinal", "nokol", "asol", "qulaity", "man kemn", "kemn product" },
                ["compare"] = new[] { "compare", "vs", "versus", "difference", "তুলনা", "কোনটা ভালো", "which one", "better", "konyta valo", "tulona", "which better", "kon ta vlo" },
                ["category"] = new[] { "category", "ক্যাটাগরি", "বিভাগ", "section", "type", "কি কি আছে", "ki ki ache", "all products", "catagory", "categori", "products gulo", "ki ase dekhi" },
                ["navigation"] = new[] { "home", "হোম", "হোম পেজ", "হোম পেজে", "হোম পেজে যেতে চাই", "হোম পেজে যেতে", "home page", "main page", "মেইন পেজ", "go home", "go to home", "ফিরে যেতে চাই", "প্রথম পেজ", "first page", "landing page", "homepage", "মূল পেজ", "main", "back to home", "হোমে যাই", "home e jai", "homepage jete chai", "মূল পাতা", "প্রথম পাতা", "home page e jete chai", "home jete chai", "home jabo", "হোমে যাব", "বাড়ি", "bari", "শুরু", "suru", "প্রধান", "pradhan", "স্টার্ট", "start page", "শুরু করুন", "শুরুতে", "shorute", "ফিরে যাব", "fire jab", "ফিরে যেতে", "fire jete", "ফিরিয়ে নিয়ে যান", "fireye nie jan", "go back", "back" },
                ["express"] = new[] { "express", "urgent", "fast", "জরুরি", "তাড়াতাড়ি", "quick delivery", "same day", "next day", "taratari", "joruri", "fast delivery", "quick drkr", "express dlvry" },
                ["membership"] = new[] { "membership", "premium", "vip", "মেম্বারশিপ", "প্রিমিয়াম", "reward", "points", "loyalty", "রিওয়ার্ড", "mmbrship", "prmium", "point", "rewrd" },
                ["seller"] = new[] { "seller", "vendor", "বিক্রেতা", "shop", "store", "দোকান", "কার কাছ থেকে", "who sells", "sellr", "dokan", "kar kase", "sellar" },
                ["complaint"] = new[] { "complaint", "অভিযোগ", "problem", "সমস্যা", "not happy", "bad", "kharap", "baje", "cheat", "scam", "ovijog", "smossa", "issue ase", "problem hoise", "bad product" },
                ["recommendation"] = new[] { "recommend", "suggestion", "সাজেশন", "রেকমেন্ড", "কি কিনব", "ki kinbo", "suggest", "ki nibo", "কি নিব", "valo product", "ভালো পণ্য", "best product", "for me", "amar jonno", "আমার জন্য", "suggest koro", "ki dekhbo", "কি দেখব", "trending", "popular item" },
                ["human_request"] = new[] { "human", "agent", "real person", "মানুষ", "এজেন্ট", "কথা বলতে চাই", "call", "connect", "transfer", "সরাসরি কথা", "manager", "ম্যানেজার", "staff", "customer service", "কাস্টমার সার্ভিস", "kotha bolte chai", "manush lagbe", "ai na", "bot na", "real human" },
                ["add_to_cart"] = new[] { "add to cart", "cart e add", "কার্টে অ্যাড", "cart add", "কিনব", "kinbo", "নিব", "nibo", "কিনতে চাই", "kinte chai", "order korbo", "অর্ডার করব", "buy this", "buy it", "eta nibo", "এটা নিব", "add koro", "অ্যাড করো", "cart e dao", "কার্টে দাও", "কিনে ফেলি", "kine feli", "purchase", "take this", "eta chai", "এটা চাই" },
                ["view_cart"] = new[] { "cart", "কার্ট", "my cart", "amar cart", "আমার কার্ট", "cart ta dekhi", "কার্ট দেখি", "cart e ki ase", "কার্টে কি আছে", "show cart", "view cart", "checkout", "চেকআউট", "কার্ট দেখাও", "আমার কার্ট দেখাও", "cart dekhao", "amar cart dekhao", "কার্ট কী", "cart ki", "কার্টে কি আছে দেখি", "আমার সব কেনার তালিকা", "এতক্ষণ কিসব কিনেছি", "টোটাল কত", "total koto", "সব মিলিয়ে", "sob miliye", "shopping list", "কেনাকাটার লিস্ট", "kenakatar list" },

                // ============ NEW ADVANCED INTENT KEYWORDS ============

                // Order Tracking - Enhanced
                ["track_order"] = new[] { "track", "tracking", "kothay order", "order koi", "order kothai", "কোথায় অর্ডার", "parcel koi", "পার্সেল কই", "order ta kothay", "amar order koi", "delivery status", "kobe asbe", "কবে আসবে", "kobe pouchabe", "কবে পৌঁছাবে", "order kobe", "tracking number", "courier status", "কোথায় আছে", "অর্ডার এখন কোথায়", "order ekhn kothay", "অর্ডার কবে দেবে", "order kobe debe", "আমার পার্সেল", "amar parcel", "প্যাকেজ ট্র্যাক করুন", "package track korun", "ডেলিভারি হয়েছে কি", "delivery hoyese ki", "পৌঁছেছে", "puncheche", "পৌঁছালো কি", "pouchchalo ki", "কবে পাব", "kobe pabo" },
                ["my_orders"] = new[] { "my orders", "amar order gulo", "আমার অর্ডার", "recent orders", "order history", "all orders", "order list", "sob order", "সব অর্ডার", "purano order", "পুরানো অর্ডার", "order dekhao", "অর্ডার দেখাও", "order gulo dekhi" },

                // Wishlist Management
                ["wishlist_add"] = new[] { "wishlist e add", "wishlist add", "save koro", "সেভ করো", "pore kinbo", "পরে কিনব", "bookmark", "বুকমার্ক", "save for later", "favorite", "ফেভারিট", "wishlist e rakho", "wishlist e dao", "পছন্দে রাখো", "posonste rakho", "save it", "eta save koro", "পছন্দে রাখতে চাই", "posondte rakte chai", "পরে কিনতে চাই", "pare kinte chai", "তালিকায় যোগ", "talicay jog", "সংরক্ষণ করুন", "songrokkhon korun", "প্রিয় তে রাখুন", "priyo te rakun", "রেখে দাও", "rekhe dao" },
                ["wishlist_view"] = new[] { "my wishlist", "wishlist", "উইশলিস্ট", "wishlist dekhi", "saved items", "সেভ করা", "favorite list", "pore kinbo list", "পছন্দের তালিকা", "posonder list", "amar wishlist", "wishlist ta dekhi", "wishlist e ki ase", "wishlist dekhao", "amar wishlist dekhao", "উইশলিস্ট দেখাও", "আমার উইশলিস্ট দেখাও", "পছন্দের জিনিস", "posondir jinis", "পরে কিনব তালিকা", "pare kinbo talika", "সংরক্ষিত পণ্য", "songrokkhit ponno", "আমার পছন্দ", "amar posond", "লিস্ট দেখুন", "list dekhun" },
                ["wishlist_remove"] = new[] { "wishlist theke remove", "wishlist remove", "উইশলিস্ট থেকে সরাও", "remove from wishlist", "delete from wishlist", "sara wishlist", "wishlist theke delete" },
                ["wishlist_to_cart"] = new[] { "wishlist to cart", "wishlist theke cart", "wishlist theke kinbo", "উইশলিস্ট থেকে কার্ট", "move to cart", "wishlist theke order", "posonder list theke cart" },

                // Coupon & Discount Discovery
                ["find_coupon"] = new[] { "coupon ase", "coupon code", "কুপন কোড", "discount code", "promo code", "offer code", "কোন কুপন", "কোন অফার", "coupon khuje dao", "coupon daw", "কুপন দাও", "discount daw", "ছাড় দাও", "ki coupon ase", "কি কুপন আছে", "available coupon", "any offer", "any discount", "kono offer ase", "kono coupon ase" },
                ["apply_coupon"] = new[] { "apply coupon", "coupon lagao", "কুপন লাগাও", "use coupon", "coupon use koro", "code apply", "apply code", "coupon ta use kori", "eta apply koro", "coupon diye discount", "কুপন দিয়ে ছাড়" },

                // Return & Refund
                ["return_request"] = new[] { "return korte chai", "রিটার্ন করতে চাই", "ferot dite chai", "ফেরত দিতে চাই", "product ferot", "পণ্য ফেরত", "return korbo", "রিটার্ন করব", "eta return", "change korte chai", "চেঞ্জ করতে চাই", "exchange korbo", "এক্সচেঞ্জ করব" },
                ["refund_status"] = new[] { "refund status", "রিফান্ড স্ট্যাটাস", "taka ferot", "টাকা ফেরত", "money back status", "refund kobe pabo", "রিফান্ড কবে পাব", "refund holo ki", "টাকা kobe diba", "taka kobe asbe", "poysa ferot kobe" },
                ["return_policy"] = new[] { "return policy", "রিটার্ন পলিসি", "ferot niti", "ফেরত নীতি", "return rules", "ki ki return jay", "কি কি রিটার্ন যায়", "koto din e return", "কতদিনে রিটার্ন", "return conditions", "return er niyom" },

                // Product Comparison
                ["compare_products"] = new[] { "compare", "তুলনা", "tulona koro", "তুলনা করো", "compare koro", "vs", "versus", "ei duita", "এই দুইটা", "difference ki", "পার্থক্য কি", "parthokko", "konyta valo", "কোনটা ভালো", "which is better", "which one better", "duita compare", "দুইটা compare", "side by side" },

                // Product Q&A
                ["product_question"] = new[] { "ei product er", "এই প্রোডাক্ট", "eta ki", "এটা কি", "product ta", "প্রোডাক্ট টা", "ei jinish", "এই জিনিস", "about this", "সম্পর্কে জানতে চাই", "jante chai", "জানতে চাই", "ki diye toiri", "কি দিয়ে তৈরি", "material ki", "ম্যাটেরিয়াল কি", "wash korte parbo", "ধোয়া যাবে", "dhoa jabe", "durable ki", "টেকসই কি", "original ki", "আসল কি" },

                // Reorder & Smart Suggestions
                ["reorder"] = new[] { "reorder", "রিঅর্ডার", "again order", "আবার অর্ডার", "ager moto", "আগের মতো", "same order", "সেম অর্ডার", "oi product ta abar", "ওই প্রোডাক্ট আবার", "last order again", "শেষ অর্ডার আবার", "abar chai", "আবার চাই", "oi ta abar", "ওইটা আবার", "eta abar", "এটা আবার" },
                ["frequently_bought"] = new[] { "frequently bought", "often buy", "সাথে কি কিনলে", "sathe ki kinle", "together kinte", "একসাথে কিনতে", "combo", "কম্বো", "set kinbo", "সেট কিনব", "bundle", "বান্ডেল", "matching", "ম্যাচিং", "goes with", "সাথে যায়" },

                // FAQ Category
                ["faq_category"] = new[] { "faq category", "faq:", "faq ", "এফএকিউ", "জনপ্রিয় বিষয়", "popular topic", "jonopriyo bishoy", "category faq", "faq er category", "faq bishoy", "faq সম্পর্কে" }
            };
        }

        #endregion

        #region Order Tracking - Real Database Queries

        private string? ExtractOrderNumber(string message)
        {
            // Pattern for order numbers like: BLY-20240115-XXXX or numeric orders
            var patterns = new[]
            {
                @"BLY-\d{8}-\d+",           // BLY-20240115-1234
                @"BLY\d{8}\d+",              // BLY202401151234
                @"#?\d{6,10}",               // #123456 or 123456789
                @"order\s*#?\s*(\d{4,10})",  // order #1234
                @"অর্ডার\s*#?\s*(\d{4,10})"  // অর্ডার #1234
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Value.Replace("#", "").Replace("order", "").Replace("অর্ডার", "").Trim();
                }
            }

            return null;
        }

        private async Task<AIChatResponse> HandleOrderTrackingAsync(string orderNumber, string? userId)
        {
            _logger.LogInformation("Looking up order: {OrderNumber} for user: {UserId}", orderNumber, userId);

            // Try to find order by order number
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Division)
                .Include(o => o.District)
                .Include(o => o.Upazila)
                .FirstOrDefaultAsync(o => o.OrderNumber != null &&
                    o.OrderNumber.Contains(orderNumber) ||
                    o.Id.ToString() == orderNumber);

            if (order == null)
            {
                // Try by ID if it's a number
                if (int.TryParse(orderNumber, out int orderId))
                {
                    order = await _db.Orders
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .Include(o => o.Division)
                        .Include(o => o.District)
                        .FirstOrDefaultAsync(o => o.Id == orderId);
                }
            }

            if (order == null)
            {
                return new AIChatResponse
                {
                    Message = $"😕 দুঃখিত! অর্ডার নম্বর **{orderNumber}** খুঁজে পাইনি।\n\n" +
                              "অনুগ্রহ করে:\n" +
                              "• অর্ডার নম্বর সঠিকভাবে লিখুন (যেমন: BLY-20240115-1234)\n" +
                              "• অথবা আপনার ইমেইল/ফোন দিয়ে অর্ডার খুঁজতে বলুন\n\n" +
                              "📞 সমস্যা হলে কল করুন আমাদের হেল্পলাইনে।"
                };
            }

            // Build detailed order status response
            var statusEmoji = GetOrderStatusEmoji(order.Status);
            var statusText = GetOrderStatusBengali(order.Status);
            var paymentStatusText = GetPaymentStatusBengali(order.PaymentStatus);
            var deliveryStatusText = GetDeliveryStatusBengali(order.DeliveryStatus);

            var itemsList = string.Join("\n", order.OrderItems.Select(oi =>
                $"  • {oi.Product?.Name ?? "পণ্য"} x{oi.Quantity} = ৳{oi.TotalPrice:N0}"));

            var locationText = "";
            if (order.District != null)
            {
                locationText = $"\n📍 ঠিকানা: {order.Address}, {order.Upazila?.Name ?? ""}, {order.District.Name}";
            }

            var trackingInfo = "";
            if (!string.IsNullOrEmpty(order.TrackingNumber))
            {
                trackingInfo = $"\n🚚 ট্র্যাকিং: {order.TrackingNumber}";
                if (!string.IsNullOrEmpty(order.CourierName))
                {
                    trackingInfo += $" ({order.CourierName})";
                }
            }

            var deliveryTimeInfo = "";
            if (order.ShippedAt.HasValue && !order.DeliveredAt.HasValue)
            {
                var daysSinceShipped = (DateTime.UtcNow - order.ShippedAt.Value).Days;
                deliveryTimeInfo = $"\n📅 শিপড হয়েছে {daysSinceShipped} দিন আগে";
            }
            else if (order.DeliveredAt.HasValue)
            {
                deliveryTimeInfo = $"\n✅ ডেলিভারি হয়েছে: {order.DeliveredAt.Value:dd MMM yyyy}";
            }

            var message = $"{statusEmoji} **অর্ডার তথ্য: {order.OrderNumber ?? $"#{order.Id}"}**\n\n" +
                          $"📦 স্ট্যাটাস: **{statusText}**\n" +
                          $"💳 পেমেন্ট: {paymentStatusText}\n" +
                          $"🚛 ডেলিভারি: {deliveryStatusText}\n" +
                          $"📅 অর্ডার তারিখ: {order.OrderDate:dd MMM yyyy, hh:mm tt}\n" +
                          $"👤 নাম: {order.CustomerName}\n" +
                          $"📱 ফোন: {order.Phone}" +
                          locationText +
                          trackingInfo +
                          deliveryTimeInfo +
                          $"\n\n🛒 **পণ্যসমূহ:**\n{itemsList}\n\n" +
                          $"💰 **মোট: ৳{order.TotalAmount:N0}**" +
                          (order.DeliveryCharge > 0 ? $" (ডেলিভারি: ৳{order.DeliveryCharge:N0})" : " (ফ্রি ডেলিভারি)");

            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed)
            {
                message += "\n\n⏳ আপনার অর্ডার প্রসেসিং হচ্ছে। শীঘ্রই শিপ করা হবে!";
            }
            else if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.OutForDelivery)
            {
                message += "\n\n🚀 আপনার অর্ডার পথে আছে! শীঘ্রই পৌঁছে যাবে।";
            }

            return new AIChatResponse { Message = message };
        }

        private async Task<AIChatResponse> HandleOrderQueryAsync(string normalizedMessage, string originalMessage, string? userId, ConversationContext context)
        {
            // Check if user mentioned order number
            var orderNumber = ExtractOrderNumber(originalMessage);
            if (!string.IsNullOrEmpty(orderNumber))
            {
                return await HandleOrderTrackingAsync(orderNumber, userId);
            }

            // If we have context from previous message
            if (!string.IsNullOrEmpty(context.LastOrderNumber))
            {
                return await HandleOrderTrackingAsync(context.LastOrderNumber, userId);
            }

            // If user is logged in, show their orders
            if (!string.IsNullOrEmpty(userId))
            {
                var recentOrders = await _db.Orders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new { o.OrderNumber, o.Id, o.Status, o.OrderDate, o.TotalAmount })
                    .ToListAsync();

                if (recentOrders.Any())
                {
                    var ordersList = string.Join("\n", recentOrders.Select(o =>
                        $"  • **{o.OrderNumber ?? $"#{o.Id}"}** - {GetOrderStatusBengali(o.Status)} - ৳{o.TotalAmount:N0} ({o.OrderDate:dd MMM})"));

                    return new AIChatResponse
                    {
                        Message = $"📦 আপনার সাম্প্রতিক অর্ডারসমূহ:\n\n{ordersList}\n\n" +
                                  "কোন অর্ডারের বিস্তারিত জানতে অর্ডার নম্বর লিখুন!"
                    };
                }
            }

            return new AIChatResponse
            {
                Message = "📦 অর্ডার ট্র্যাক করতে আপনার অর্ডার নম্বর লিখুন!\n\n" +
                          "**উদাহরণ:**\n" +
                          "• BLY-20240115-1234\n" +
                          "• অথবা শুধু নম্বর: 123456\n\n" +
                          "অর্ডার নম্বর পাবেন:\n" +
                          "• অর্ডার কনফার্মেশন ইমেইলে\n" +
                          "• SMS এ\n" +
                          "• আপনার অ্যাকাউন্টে লগইন করে"
            };
        }

        private string GetOrderStatusEmoji(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "⏳",
            OrderStatus.Confirmed => "✅",
            OrderStatus.Processing => "⚙️",
            OrderStatus.Shipped => "📦",
            OrderStatus.OutForDelivery => "🚚",
            OrderStatus.Delivered => "🎉",
            OrderStatus.Cancelled => "❌",
            OrderStatus.Returned => "↩️",
            OrderStatus.Refunded => "💸",
            _ => "📋"
        };

        private string GetOrderStatusBengali(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "পেন্ডিং",
            OrderStatus.Confirmed => "কনফার্মড",
            OrderStatus.Processing => "প্রসেসিং",
            OrderStatus.Shipped => "শিপড",
            OrderStatus.OutForDelivery => "ডেলিভারির পথে",
            OrderStatus.Delivered => "ডেলিভার্ড ✓",
            OrderStatus.Cancelled => "বাতিল",
            OrderStatus.Returned => "রিটার্নড",
            OrderStatus.Refunded => "রিফান্ডেড",
            _ => "অজানা"
        };

        private string GetPaymentStatusBengali(PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "বাকি আছে",
            PaymentStatus.Processing => "প্রসেসিং",
            PaymentStatus.Completed => "সম্পন্ন ✓",
            PaymentStatus.Failed => "ব্যর্থ",
            PaymentStatus.Refunded => "রিফান্ডেড",
            _ => "অজানা"
        };

        private string GetDeliveryStatusBengali(DeliveryStatus status) => status switch
        {
            DeliveryStatus.Pending => "প্রস্তুত হচ্ছে",
            DeliveryStatus.Processing => "প্যাকেজিং",
            DeliveryStatus.Shipped => "পথে আছে",
            DeliveryStatus.InTransit => "ট্রানজিটে",
            DeliveryStatus.OutForDelivery => "আজকে পৌঁছাবে",
            DeliveryStatus.Delivered => "ডেলিভার্ড ✓",
            DeliveryStatus.Failed => "ব্যর্থ",
            DeliveryStatus.Returned => "ফেরত",
            _ => "অজানা"
        };

        #endregion

        #region Product Search - Real Database Queries

        private string? ExtractProductQuery(string normalizedMessage, string originalMessage)
        {
            // Remove common intent keywords to get product name
            // Include both English/Banglish and Bengali keywords
            var removeWords = new[] {
                // English/Banglish
                "product", "item", "buy", "price", "cost", "show", "dekhao", "dekhan", "kinbo", "chai", "lagbe", "dam", "koto", "ki", "ase", "stock", "available", "size", "color", "colour", "me", "all", "new", "best", "seller", "category", "products", "items",
                // Bengali
                "পণ্য", "দেখাও", "দেখান", "দেখি", "কিনতে", "চাই", "লাগবে", "দাম", "কত", "কি", "আছে", "সব", "নতুন", "বেস্ট", "সেলার", "ক্যাটাগরি", "দেখুন", "আরো", "আরও"
            };

            var words = originalMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !removeWords.Contains(w.ToLower()) && w.Length > 2)
                .ToList();

            return words.Count > 0 ? string.Join(" ", words) : null;
        }

        private async Task<AIChatResponse> HandleProductSearchAsync(string query, string originalMessage)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Show random featured products
                var featuredProducts = await _db.Products
                    .Where(p => p.Status == ProductStatus.Active && p.IsFeatured)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(5)
                    .Select(p => new AIProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl,
                        Slug = p.Slug
                    })
                    .ToListAsync();

                if (featuredProducts.Any())
                {
                    var productList = string.Join("\n", featuredProducts.Select(p =>
                        $"  🛍️ **{p.Name}**\n     💰 {(p.DiscountPrice.HasValue ? $"~~৳{p.Price:N0}~~ ৳{p.DiscountPrice:N0}" : $"৳{p.Price:N0}")}"));

                    return new AIChatResponse
                    {
                        Message = $"🌟 আমাদের ফিচার্ড পণ্যসমূহ:\n\n{productList}\n\n" +
                                  "কোন পণ্য দেখতে চান? নাম লিখুন অথবা ক্যাটাগরি বলুন!",
                        ProductSuggestions = featuredProducts
                    };
                }
            }

            // Search products with fuzzy matching
            var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.Status == ProductStatus.Active &&
                    (p.Name.ToLower().Contains(query.ToLower()) ||
                     p.ShortDescription != null && p.ShortDescription.ToLower().Contains(query.ToLower()) ||
                     p.Tags != null && p.Tags.ToLower().Contains(query.ToLower()) ||
                     p.Category != null && p.Category.Name.ToLower().Contains(query.ToLower()) ||
                     searchTerms.Any(t => p.Name.ToLower().Contains(t))))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.SoldCount)
                .Take(6)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.DiscountPrice,
                    p.ImageUrl,
                    p.Slug,
                    p.Stock,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    p.ShortDescription
                })
                .ToListAsync();

            if (!products.Any())
            {
                // Try category search
                var categories = await _db.Categories
                    .Where(c => c.IsActive && c.Name.ToLower().Contains(query.ToLower()))
                    .Take(3)
                    .ToListAsync();

                if (categories.Any())
                {
                    var catList = string.Join(", ", categories.Select(c => $"**{c.Name}**"));
                    return new AIChatResponse
                    {
                        Message = $"'{query}' নামে পণ্য পাইনি, তবে এই ক্যাটাগরি পেয়েছি: {catList}\n\n" +
                                  "এই ক্যাটাগরির পণ্য দেখতে চাইলে ক্যাটাগরির নাম লিখুন!"
                    };
                }

                return new AIChatResponse
                {
                    Message = $"😕 দুঃখিত! '{query}' খুঁজে পাইনি।\n\n" +
                              "চেষ্টা করুন:\n" +
                              "• অন্য কীওয়ার্ড দিয়ে সার্চ করুন\n" +
                              "• 'category' লিখে সব ক্যাটাগরি দেখুন\n" +
                              "• 'new' লিখে নতুন পণ্য দেখুন"
                };
            }

            var productSuggestions = products.Select(p => new AIProductSuggestion
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                ImageUrl = p.ImageUrl,
                Slug = p.Slug
            }).ToList();

            var productListText = string.Join("\n\n", products.Select(p =>
            {
                var priceText = p.DiscountPrice.HasValue
                    ? $"~~৳{p.Price:N0}~~ **৳{p.DiscountPrice:N0}** ({(int)((p.Price - p.DiscountPrice.Value) / p.Price * 100)}% ছাড়!)"
                    : $"**৳{p.Price:N0}**";
                var stockText = p.Stock > 0 ? $"✅ স্টকে আছে ({p.Stock}টি)" : "❌ স্টক আউট";
                var catText = p.CategoryName != null ? $" | 📁 {p.CategoryName}" : "";

                return $"🛍️ **{p.Name}**\n   {priceText} {catText}\n   {stockText}";
            }));

            return new AIChatResponse
            {
                Message = $"🔍 **'{query}' এর জন্য {products.Count}টি পণ্য পেয়েছি:**\n\n{productListText}\n\n" +
                          "বিস্তারিত জানতে পণ্যের নাম লিখুন অথবা ওয়েবসাইটে দেখুন!",
                ProductSuggestions = productSuggestions
            };
        }

        private async Task<AIChatResponse> HandlePriceQueryAsync(string query, string originalMessage, ConversationContext context)
        {
            // Use context if query is empty
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                if (!string.IsNullOrEmpty(context.LastProductQuery))
                {
                    query = context.LastProductQuery;
                }
                else
                {
                    return new AIChatResponse
                    {
                        Message = "কোন পণ্যের দাম জানতে চান? পণ্যের নাম লিখুন! 🛍️\n\n" +
                                  "যেমন: 'শাড়ি দাম কত' অথবা 'tshirt price'"
                    };
                }
            }

            return await HandleProductSearchAsync(query, originalMessage);
        }

        #endregion

        #region FAQ Matching

        private async Task<FAQ?> FindMatchingFAQAsync(string normalizedMessage, string originalMessage)
        {
            var faqs = await _db.FAQs
                .Where(f => f.IsActive)
                .ToListAsync();

            if (!faqs.Any()) return null;

            FAQ? bestMatch = null;
            double bestScore = 0;

            foreach (var faq in faqs)
            {
                var faqQuestion = NormalizeBanglish(faq.Question.ToLower());
                var similarity = CalculateSimilarity(normalizedMessage, faqQuestion);

                // Also check for keyword matches
                var faqWords = faqQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var messageWords = normalizedMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var commonWords = faqWords.Intersect(messageWords).Count();
                var keywordScore = (double)commonWords / Math.Max(faqWords.Length, messageWords.Length);

                var totalScore = (similarity * 0.6) + (keywordScore * 0.4);

                if (totalScore > bestScore && totalScore > 0.4)
                {
                    bestScore = totalScore;
                    bestMatch = faq;
                }
            }

            return bestMatch;
        }

        private async Task<AIChatResponse> HandleFaqCategoryIntentAsync(string normalizedMessage, string originalMessage)
        {
            // Extract category name from message
            var categoryName = ExtractFaqCategory(normalizedMessage, originalMessage);

            if (string.IsNullOrEmpty(categoryName))
            {
                // Show all FAQ categories
                var allCategories = await _db.FAQs
                    .Where(f => f.IsActive && f.Category != null)
                    .Select(f => f.Category)
                    .Distinct()
                    .ToListAsync();

                if (!allCategories.Any())
                {
                    return new AIChatResponse
                    {
                        Message = "😊 এই মুহূর্তে কোনো FAQ ক্যাটাগরি নেই।"
                    };
                }

                var quickReplies = allCategories.Select(cat => new QuickReplyButton
                {
                    Text = $"📚 {cat}",
                    Action = "send_message",
                    Payload = $"faq category {cat}",
                    Style = "default"
                }).ToList();

                return new AIChatResponse
                {
                    Message = "📚 **FAQ ক্যাটাগরি সমূহ:**\n\nকোন বিষয়ে জানতে চান?",
                    QuickReplies = quickReplies
                };
            }

            // Find FAQs for the specific category
            var faqs = await _db.FAQs
                .Where(f => f.IsActive && f.Category != null &&
                       f.Category.ToLower().Contains(categoryName.ToLower()))
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();

            if (!faqs.Any())
            {
                return new AIChatResponse
                {
                    Message = $"😊 **{categoryName}** ক্যাটাগরিতে কোনো FAQ নেই।\n\nঅন্য বিষয়ে জানতে চাইলে বলুন!"
                };
            }

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"📚 **{faqs.First().Category}** সম্পর্কে প্রশ্নোত্তর:\n");

            foreach (var faq in faqs)
            {
                messageBuilder.AppendLine($"❓ **{faq.Question}**");
                messageBuilder.AppendLine($"✅ {faq.Answer}\n");
            }

            messageBuilder.AppendLine("---");
            messageBuilder.AppendLine("আরো কোনো প্রশ্ন থাকলে জিজ্ঞেস করুন! 😊");

            // Get other categories for quick replies
            var otherCategories = await _db.FAQs
                .Where(f => f.IsActive && f.Category != null &&
                       !f.Category.ToLower().Contains(categoryName.ToLower()))
                .Select(f => f.Category)
                .Distinct()
                .Take(3)
                .ToListAsync();

            var quickRepliesOther = otherCategories.Select(cat => new QuickReplyButton
            {
                Text = $"📚 {cat}",
                Action = "send_message",
                Payload = $"faq category {cat}",
                Style = "default"
            }).ToList();

            return new AIChatResponse
            {
                Message = messageBuilder.ToString(),
                QuickReplies = quickRepliesOther
            };
        }

        private string? ExtractFaqCategory(string normalizedMessage, string originalMessage)
        {
            // Try to extract category from patterns like "faq category Test" or "faq Test"
            var patterns = new[]
            {
                @"faq\s+category\s+(.+)",
                @"faq:\s*(.+)",
                @"faq\s+(.+)",
                @"জনপ্রিয় বিষয়\s+(.+)",
                @"এফএকিউ\s+(.+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(originalMessage, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }

                match = Regex.Match(normalizedMessage, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return null;
        }

        #endregion

        #region Category & Product Handlers

        private async Task<AIChatResponse> HandleCategoryQueryAsync(string message, ConversationContext context)
        {
            var categories = await _db.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.DisplayOrder)
                .Include(c => c.Products.Where(p => p.Status == ProductStatus.Active))
                .Include(c => c.Children.Where(ch => ch.IsActive))
                    .ThenInclude(ch => ch.Products.Where(p => p.Status == ProductStatus.Active))
                .ToListAsync();

            if (!categories.Any())
            {
                return new AIChatResponse
                {
                    Message = "দুঃখিত! বর্তমানে কোনো ক্যাটাগরি পাওয়া যায়নি। শীঘ্রই আপডেট করা হবে!"
                };
            }

            var categoryList = string.Join("\n", categories.Select(c =>
            {
                // Count direct products + subcategory products
                var directProducts = c.Products.Count;
                var subcategoryProducts = c.Children.Sum(ch => ch.Products.Count);
                var totalProductCount = directProducts + subcategoryProducts;
                var subCatCount = c.Children.Count;
                var subText = subCatCount > 0 ? $" ({subCatCount} সাব-ক্যাটাগরি)" : "";
                return $"📁 **{c.Name}** - {totalProductCount}টি পণ্য{subText}";
            }));

            // Check if user is asking about a specific category from context
            var contextHint = !string.IsNullOrEmpty(context.LastCategory)
                ? $"\n\n💡 আগে আপনি **{context.LastCategory}** দেখছিলেন।"
                : "";

            return new AIChatResponse
            {
                Message = $"🏪 আমাদের ক্যাটাগরিসমূহ:\n\n{categoryList}{contextHint}\n\n" +
                          "কোন ক্যাটাগরির পণ্য দেখতে চান? নাম লিখুন!"
            };
        }

        private async Task<AIChatResponse> HandleNewArrivalQueryAsync()
        {
            var newProducts = await _db.Products
                .Where(p => p.Status == ProductStatus.Active)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .Select(p => new
                {
                    p.Name,
                    p.Price,
                    p.DiscountPrice,
                    p.CreatedAt
                })
                .ToListAsync();

            if (!newProducts.Any())
            {
                return new AIChatResponse
                {
                    Message = "নতুন পণ্য শীঘ্রই আসছে! 🎉 চোখ রাখুন!"
                };
            }

            var productList = string.Join("\n", newProducts.Select(p =>
            {
                var daysAgo = (DateTime.UtcNow - p.CreatedAt).Days;
                var timeText = daysAgo == 0 ? "আজকে এসেছে!" : daysAgo == 1 ? "গতকাল এসেছে" : $"{daysAgo} দিন আগে";
                var priceText = p.DiscountPrice.HasValue ? $"~~৳{p.Price:N0}~~ ৳{p.DiscountPrice:N0}" : $"৳{p.Price:N0}";
                return $"🆕 **{p.Name}** - {priceText}\n   ⏰ {timeText}";
            }));

            return new AIChatResponse
            {
                Message = $"🌟 নতুন এসেছে:\n\n{productList}\n\n" +
                          "কোনটা দেখবেন? নাম লিখুন!"
            };
        }

        private async Task<AIChatResponse> HandleBestSellerQueryAsync()
        {
            var bestSellers = await _db.Products
                .Where(p => p.Status == ProductStatus.Active && p.SoldCount > 0)
                .OrderByDescending(p => p.SoldCount)
                .Take(6)
                .Select(p => new
                {
                    p.Name,
                    p.Price,
                    p.DiscountPrice,
                    p.SoldCount
                })
                .ToListAsync();

            if (!bestSellers.Any())
            {
                // Show featured products instead
                var featured = await _db.Products
                    .Where(p => p.Status == ProductStatus.Active && p.IsFeatured)
                    .Take(5)
                    .Select(p => new { p.Name, p.Price, p.DiscountPrice })
                    .ToListAsync();

                if (featured.Any())
                {
                    var featuredList = string.Join("\n", featured.Select(p =>
                        $"⭐ **{p.Name}** - {(p.DiscountPrice.HasValue ? $"৳{p.DiscountPrice:N0}" : $"৳{p.Price:N0}")}"));

                    return new AIChatResponse
                    {
                        Message = $"🌟 আমাদের ফিচার্ড পণ্য:\n\n{featuredList}"
                    };
                }

                return new AIChatResponse
                {
                    Message = "এই মুহূর্তে বেস্ট সেলার তথ্য আপডেট হচ্ছে। 'new' লিখে নতুন পণ্য দেখুন!"
                };
            }

            var productList = string.Join("\n", bestSellers.Select(p =>
            {
                var priceText = p.DiscountPrice.HasValue ? $"~~৳{p.Price:N0}~~ ৳{p.DiscountPrice:N0}" : $"৳{p.Price:N0}";
                return $"🔥 **{p.Name}** - {priceText}\n   📈 {p.SoldCount}টি বিক্রি হয়েছে";
            }));

            return new AIChatResponse
            {
                Message = $"🏆 বেস্ট সেলার পণ্যসমূহ:\n\n{productList}\n\n" +
                          "এগুলো সবচেয়ে জনপ্রিয়! কোনটা নেবেন?"
            };
        }

        private async Task<AIChatResponse> HandleStockQueryAsync(string query, string originalMessage, ConversationContext context)
        {
            // Use context if query is empty
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                if (!string.IsNullOrEmpty(context.LastProductQuery))
                {
                    query = context.LastProductQuery;
                }
                else
                {
                    return new AIChatResponse
                    {
                        Message = "কোন পণ্যের স্টক জানতে চান? পণ্যের নাম লিখুন!\n\n" +
                                  "যেমন: 'শাড়ি stock আছে?' অথবা 'tshirt available?'"
                    };
                }
            }

            var products = await _db.Products
                .Where(p => p.Status == ProductStatus.Active &&
                    (p.Name.ToLower().Contains(query.ToLower()) ||
                     p.Tags != null && p.Tags.ToLower().Contains(query.ToLower())))
                .Take(5)
                .Select(p => new { p.Name, p.Stock, p.Price })
                .ToListAsync();

            if (!products.Any())
            {
                return new AIChatResponse
                {
                    Message = $"'{query}' নামে পণ্য খুঁজে পাইনি। অন্য নাম দিয়ে চেষ্টা করুন!"
                };
            }

            var stockList = string.Join("\n", products.Select(p =>
            {
                var stockStatus = p.Stock > 10 ? $"✅ প্রচুর স্টক ({p.Stock}টি)"
                    : p.Stock > 0 ? $"⚠️ অল্প স্টক ({p.Stock}টি বাকি)"
                    : "❌ স্টক আউট";
                return $"📦 **{p.Name}** - {stockStatus}";
            }));

            return new AIChatResponse
            {
                Message = $"📊 স্টক তথ্য:\n\n{stockList}\n\n" +
                          "দ্রুত অর্ডার করুন - জনপ্রিয় পণ্য তাড়াতাড়ি শেষ হয়ে যায়!"
            };
        }

        private async Task<AIChatResponse> HandleSizeQueryAsync(string? productQuery, ConversationContext context)
        {
            // Use context if no product query
            if (string.IsNullOrEmpty(productQuery) && !string.IsNullOrEmpty(context.LastProductQuery))
            {
                productQuery = context.LastProductQuery;
            }

            // If we have a product query, try to show specific product sizes
            if (!string.IsNullOrEmpty(productQuery))
            {
                var products = await _db.Products
                    .Include(p => p.Variants)
                    .Where(p => p.Status == ProductStatus.Active &&
                        p.Name.ToLower().Contains(productQuery.ToLower()))
                    .Take(3)
                    .ToListAsync();

                if (products.Any())
                {
                    var sizeInfo = new System.Text.StringBuilder();
                    sizeInfo.AppendLine($"📏 **\"{productQuery}\" এর সাইজ তথ্য:**\n");

                    foreach (var product in products)
                    {
                        sizeInfo.AppendLine($"🛍️ **{product.Name}**");
                        if (product.Variants?.Any() == true)
                        {
                            var sizes = product.Variants
                                .Where(v => !string.IsNullOrEmpty(v.Size))
                                .Select(v => $"• {v.Size} ({(v.Stock > 0 ? "✅ আছে" : "❌ নেই")})")
                                .Distinct();
                            sizeInfo.AppendLine(string.Join("\n", sizes));
                        }
                        else
                        {
                            sizeInfo.AppendLine("• ফ্রি সাইজ");
                        }
                        sizeInfo.AppendLine();
                    }

                    return new AIChatResponse { Message = sizeInfo.ToString() };
                }
            }

            var generalSizeInfo = @"📏 **সাইজ গাইড:**

👕 **টপস/শার্ট:**
• S = বুক ৩৪-৩৬""
• M = বুক ৩৮-৪০""
• L = বুক ৪২-৪৪""
• XL = বুক ৪৬-৪৮""
• XXL = বুক ৫০-৫২""

👖 **প্যান্ট/বটম:**
• S = কোমর ২৮-৩০""
• M = কোমর ৩২-৩৪""
• L = কোমর ৩৬-৩৮""
• XL = কোমর ৪০-৪২""

👗 **শাড়ি:**
• স্ট্যান্ডার্ড: ৫.৫ মিটার (ব্লাউজ পিস সহ ৬.২৫ মিটার)

💡 **টিপস:** সঠিক সাইজ পেতে পণ্যের পেজে সাইজ চার্ট দেখুন!

কোন পণ্যের সাইজ জানতে চান?";

            return new AIChatResponse { Message = generalSizeInfo };
        }

        private async Task<AIChatResponse> HandleColorQueryAsync(string? productQuery, ConversationContext context)
        {
            // Use context if no product query
            if (string.IsNullOrEmpty(productQuery) && !string.IsNullOrEmpty(context.LastProductQuery))
            {
                productQuery = context.LastProductQuery;
            }

            if (!string.IsNullOrEmpty(productQuery))
            {
                var products = await _db.Products
                    .Include(p => p.Variants)
                    .Where(p => p.Status == ProductStatus.Active &&
                        p.Name.ToLower().Contains(productQuery.ToLower()))
                    .Take(3)
                    .ToListAsync();

                if (products.Any())
                {
                    var colorInfo = products.Select(p =>
                    {
                        var colors = p.Variants
                            .Where(v => !string.IsNullOrEmpty(v.Color))
                            .Select(v => v.Color)
                            .Distinct()
                            .ToList();

                        var colorText = colors.Any() ? string.Join(", ", colors) : "স্ট্যান্ডার্ড কালার";
                        return $"🎨 **{p.Name}**: {colorText}";
                    });

                    return new AIChatResponse
                    {
                        Message = $"🌈 কালার তথ্য:\n\n{string.Join("\n", colorInfo)}\n\n" +
                                  "বিস্তারিত দেখতে পণ্যের পেজে যান!"
                    };
                }
            }

            return new AIChatResponse
            {
                Message = "🎨 কোন পণ্যের কালার জানতে চান?\n\nপণ্যের নাম লিখুন, আমি কালার অপশনগুলো দেখাব!"
            };
        }

        private async Task<AIChatResponse> HandleReviewQueryAsync(string? productQuery, ConversationContext context)
        {
            // Use context if no product query
            if (string.IsNullOrEmpty(productQuery) && !string.IsNullOrEmpty(context.LastProductQuery))
            {
                productQuery = context.LastProductQuery;
            }

            if (!string.IsNullOrEmpty(productQuery))
            {
                var productWithReviews = await _db.Products
                    .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .Where(p => p.Status == ProductStatus.Active &&
                        p.Name.ToLower().Contains(productQuery.ToLower()))
                    .FirstOrDefaultAsync();

                if (productWithReviews != null)
                {
                    var reviews = productWithReviews.Reviews.Take(3).ToList();
                    var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                    var totalReviews = productWithReviews.Reviews.Count;

                    var reviewList = reviews.Select(r =>
                        $"⭐ {r.Rating}/5 - \"{(r.Comment?.Length > 50 ? r.Comment.Substring(0, 50) + "..." : r.Comment ?? "ভালো পণ্য")}\"");

                    return new AIChatResponse
                    {
                        Message = $"📝 **{productWithReviews.Name}** এর রিভিউ:\n\n" +
                                  $"⭐ গড় রেটিং: **{avgRating:F1}/5** ({totalReviews}টি রিভিউ)\n\n" +
                                  $"{string.Join("\n", reviewList)}\n\n" +
                                  "বিস্তারিত রিভিউ দেখতে পণ্যের পেজে যান!"
                    };
                }
            }

            // General review info
            var topRatedProducts = await _db.Products
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .Where(p => p.Status == ProductStatus.Active && p.Reviews.Any())
                .OrderByDescending(p => p.Reviews.Average(r => r.Rating))
                .Take(5)
                .Select(p => new
                {
                    p.Name,
                    AvgRating = p.Reviews.Average(r => r.Rating),
                    ReviewCount = p.Reviews.Count
                })
                .ToListAsync();

            if (topRatedProducts.Any())
            {
                var productList = string.Join("\n", topRatedProducts.Select(p =>
                    $"⭐ {p.AvgRating:F1}/5 - **{p.Name}** ({p.ReviewCount} রিভিউ)"));

                return new AIChatResponse
                {
                    Message = $"🏅 টপ রেটেড পণ্য:\n\n{productList}\n\n" +
                              "কোন পণ্যের রিভিউ দেখতে চান? নাম লিখুন!"
                };
            }

            return new AIChatResponse
            {
                Message = "📝 রিভিউ দেখতে পণ্যের নাম লিখুন!\n\n" +
                          "আমরা সব পণ্যে সৎ কাস্টমার রিভিউ দেখাই। 💯"
            };
        }

        #endregion

        #region Payment & Shipping Handlers

        private async Task<AIChatResponse> HandlePaymentQueryAsync()
        {
            var settings = await _siteSettingsService.GetSiteSettingsAsync();

            var paymentMethods = new List<string>();

            if (!string.IsNullOrEmpty(settings?.BkashNumber))
                paymentMethods.Add($"📱 **বিকাশ:** {settings.BkashNumber}");
            if (!string.IsNullOrEmpty(settings?.NagadNumber))
                paymentMethods.Add($"📱 **নগদ:** {settings.NagadNumber}");
            if (!string.IsNullOrEmpty(settings?.RocketNumber))
                paymentMethods.Add($"📱 **রকেট:** {settings.RocketNumber}");
            if (!string.IsNullOrEmpty(settings?.UpayNumber))
                paymentMethods.Add($"📱 **উপায়:** {settings.UpayNumber}");

            var mfsNumbers = paymentMethods.Any()
                ? string.Join("\n", paymentMethods)
                : "📱 MFS নম্বর শীঘ্রই আপডেট হবে";

            return new AIChatResponse
            {
                Message = $"💳 **পেমেন্ট অপশন:**\n\n" +
                          $"**মোবাইল ব্যাংকিং:**\n{mfsNumbers}\n\n" +
                          "**অনলাইন পেমেন্ট:**\n" +
                          "💳 VISA/Mastercard (SSLCommerz)\n" +
                          "🏦 ইন্টারনেট ব্যাংকিং\n\n" +
                          "**ক্যাশ অন ডেলিভারি:**\n" +
                          "🏠 হাতে নিয়ে পেমেন্ট (অতিরিক্ত চার্জ প্রযোজ্য হতে পারে)\n\n" +
                          "💡 **টিপস:** বিকাশ/নগদে আগাম পেমেন্টে বিশেষ ছাড় পেতে পারেন!"
            };
        }

        private async Task<AIChatResponse> HandleShippingQueryAsync()
        {
            var settings = await _siteSettingsService.GetSiteSettingsAsync();
            var deliveryCharge = settings?.DefaultDeliveryCharge ?? 100;
            var freeDeliveryThreshold = settings?.FreeDeliveryThreshold;

            var freeDeliveryText = freeDeliveryThreshold.HasValue
                ? $"\n\n🎁 **ফ্রি ডেলিভারি:** ৳{freeDeliveryThreshold:N0}+ অর্ডারে!"
                : "";

            // Get district-wise delivery charges
            var districtCharges = await _db.DistrictDeliveryCharges
                .Include(d => d.District)
                .Take(5)
                .ToListAsync();

            var districtInfo = districtCharges.Any()
                ? "\n\n📍 **এলাকা ভিত্তিক চার্জ:**\n" +
                  string.Join("\n", districtCharges.Select(d =>
                      $"  • {d.District?.Name ?? "অন্যান্য"}: ৳{d.DeliveryCharge:N0}"))
                : "";

            return new AIChatResponse
            {
                Message = $"🚚 **ডেলিভারি তথ্য:**\n\n" +
                          $"📦 সাধারণ ডেলিভারি চার্জ: **৳{deliveryCharge:N0}**\n" +
                          $"⏰ ডেলিভারি সময়: ঢাকায় ২-৩ দিন, ঢাকার বাইরে ৩-৫ দিন" +
                          freeDeliveryText +
                          districtInfo +
                          "\n\n🚀 এক্সপ্রেস ডেলিভারি সম্পর্কে জানতে 'express' লিখুন!"
            };
        }

        private async Task<AIChatResponse> HandleExpressDeliveryQueryAsync()
        {
            return new AIChatResponse
            {
                Message = "🚀 **এক্সপ্রেস ডেলিভারি সার্ভিস:**\n\n" +
                          "⚡ **সেম ডে ডেলিভারি:**\n" +
                          "  • শুধু ঢাকা সিটিতে\n" +
                          "  • দুপুর ১২টার আগে অর্ডার করলে\n" +
                          "  • অতিরিক্ত: ৳100-150\n\n" +
                          "🌅 **নেক্সট ডে ডেলিভারি:**\n" +
                          "  • ঢাকা ও চট্টগ্রামে\n" +
                          "  • বিকাল ৫টার আগে অর্ডার করলে\n" +
                          "  • অতিরিক্ত: ৳50-80\n\n" +
                          "📞 এক্সপ্রেস ডেলিভারি নিশ্চিত করতে অর্ডারের সময় নোটে উল্লেখ করুন!"
            };
        }

        private async Task<AIChatResponse> HandleCODQueryAsync()
        {
            var settings = await _siteSettingsService.GetSiteSettingsAsync();

            return new AIChatResponse
            {
                Message = "🏠 **ক্যাশ অন ডেলিভারি (COD):**\n\n" +
                          "✅ হ্যাঁ, আমরা COD সাপোর্ট করি!\n\n" +
                          "**নিয়মাবলী:**\n" +
                          "• পণ্য হাতে নিয়ে টাকা দিতে পারবেন\n" +
                          "• কুরিয়ার বয়কে সঠিক পরিমাণ দিন\n" +
                          "• অতিরিক্ত COD চার্জ: ১%-২% (পণ্য ভেদে)\n\n" +
                          "⚠️ **সতর্কতা:**\n" +
                          "• পণ্য না নিলে একাউন্টে মার্ক হবে\n" +
                          "• বারবার না নিলে COD সুবিধা বন্ধ হতে পারে\n\n" +
                          "💡 **টিপস:** আগে পেমেন্টে বিশেষ ছাড় পেতে পারেন!"
            };
        }

        private async Task<AIChatResponse> HandleReturnRefundQueryAsync()
        {
            return new AIChatResponse
            {
                Message = "↩️ **রিটার্ন ও রিফান্ড পলিসি:**\n\n" +
                          "📅 **রিটার্ন সময়সীমা:** ডেলিভারির ৭ দিনের মধ্যে\n\n" +
                          "✅ **যেসব কারণে রিটার্ন হয়:**\n" +
                          "• ভুল পণ্য ডেলিভারি\n" +
                          "• ড্যামেজড/নষ্ট পণ্য\n" +
                          "• সাইজ মিলছে না (এক্সচেঞ্জ)\n" +
                          "• পণ্যের বর্ণনার সাথে মিল নেই\n\n" +
                          "❌ **যা রিটার্ন হয় না:**\n" +
                          "• ব্যবহার করা পণ্য\n" +
                          "• ট্যাগ ছেঁড়া/সরানো\n" +
                          "• মন পরিবর্তন\n\n" +
                          "💰 **রিফান্ড অপশন:**\n" +
                          "• ওয়ালেট ক্রেডিট (দ্রুত)\n" +
                          "• বিকাশ/নগদ (৩-৫ কার্যদিবস)\n\n" +
                          "📞 রিটার্ন রিকোয়েস্ট করতে 'complaint' লিখুন বা হেল্পলাইনে কল করুন!"
            };
        }

        private async Task<AIChatResponse> HandleDiscountQueryAsync(string? userId)
        {
            // Get active coupons - only public (AssignedToUserId is null) + user's own coupons
            var activeCoupons = await _db.Coupons
                .Where(c => c.IsActive &&
                    (c.StartDate == null || c.StartDate <= DateTime.UtcNow) &&
                    (c.EndDate == null || c.EndDate >= DateTime.UtcNow) &&
                    (c.UsageLimit == null || c.TimesUsed < c.UsageLimit) &&
                    // Filter: Public coupons (no user assigned) OR assigned to current user
                    (c.AssignedToUserId == null || c.AssignedToUserId == userId))
                .Take(5)
                .ToListAsync();

            var couponList = activeCoupons.Any()
                ? "🎟️ **চলমান কুপন:**\n" +
                  string.Join("\n", activeCoupons.Select(c =>
                  {
                      var discountText = c.DiscountType == DiscountType.Percentage
                          ? $"{c.DiscountValue}% ছাড়"
                          : $"৳{c.DiscountValue:N0} ছাড়";
                      var minOrder = c.MinimumOrderAmount.HasValue ? $" (৳{c.MinimumOrderAmount:N0}+ অর্ডারে)" : "";
                      return $"  • **{c.Code}** - {discountText}{minOrder}";
                  }))
                : "🎟️ এই মুহূর্তে কোনো পাবলিক কুপন নেই।";

            // Get discounted products count
            var discountedProducts = await _db.Products
                .CountAsync(p => p.Status == ProductStatus.Active && p.DiscountPrice.HasValue && p.DiscountPrice < p.Price);

            return new AIChatResponse
            {
                Message = $"🎉 **অফার ও ডিসকাউন্ট:**\n\n" +
                          $"{couponList}\n\n" +
                          $"🏷️ **ডিসকাউন্টেড পণ্য:** {discountedProducts}+ পণ্যে সরাসরি ছাড়!\n\n" +
                          "💡 **কুপন ব্যবহার করবেন কিভাবে?**\n" +
                          "চেকআউটের সময় কুপন কোড দিন এবং 'Apply' চাপুন!\n\n" +
                          "🔥 সেল পণ্য দেখতে 'sale' বা 'discount' সার্চ করুন!"
            };
        }

        #endregion

        #region Support & Contact Handlers

        private async Task<AIChatResponse> HandleContactQueryAsync()
        {
            var settings = await _siteSettingsService.GetSiteSettingsAsync();

            return new AIChatResponse
            {
                Message = $"📞 **যোগাযোগ করুন:**\n\n" +
                          (settings?.ContactPhone != null ? $"📱 ফোন: **{settings.ContactPhone}**\n" : "") +
                          (settings?.ContactEmail != null ? $"📧 ইমেইল: **{settings.ContactEmail}**\n" : "") +
                          (settings?.ContactAddress != null ? $"📍 ঠিকানা: {settings.ContactAddress}\n" : "") +
                          "\n⏰ **সার্ভিস সময়:**\n" +
                          "সকাল ৯টা - রাত ১০টা (সপ্তাহে ৭ দিন)\n\n" +
                          "💬 এই চ্যাটেও আমি ২৪/৭ আছি! কি সাহায্য লাগবে?"
            };
        }

        private async Task<AIChatResponse> HandleMembershipQueryAsync()
        {
            return new AIChatResponse
            {
                Message = "👑 **প্রিমিয়াম মেম্বারশিপ:**\n\n" +
                          "✨ **সুবিধাসমূহ:**\n" +
                          "• প্রতি অর্ডারে এক্সট্রা ৫% ছাড়\n" +
                          "• ফ্রি এক্সপ্রেস ডেলিভারি\n" +
                          "• আর্লি এক্সেস টু সেলস\n" +
                          "• প্রায়োরিটি কাস্টমার সাপোর্ট\n" +
                          "• বার্থডে স্পেশাল অফার\n\n" +
                          "🎁 **রিওয়ার্ড পয়েন্ট:**\n" +
                          "• প্রতি ৳১০০ = ১ পয়েন্ট\n" +
                          "• ১০০ পয়েন্ট = ৳৫০ ভাউচার\n" +
                          "• রিভিউ দিলে বোনাস পয়েন্ট!\n\n" +
                          "📝 মেম্বার হতে একাউন্ট খুলুন এবং শপিং শুরু করুন!"
            };
        }

        private async Task<AIChatResponse> HandleSellerQueryAsync(string message)
        {
            var sellerCount = await _db.Sellers.CountAsync(s => s.IsVerified && s.Status == SellerStatus.Approved);

            return new AIChatResponse
            {
                Message = $"🏪 **আমাদের মার্কেটপ্লেস:**\n\n" +
                          $"✅ {sellerCount}+ ভেরিফাইড সেলার\n" +
                          "✅ সব পণ্য কোয়ালিটি চেক করা\n" +
                          "✅ সেলার রেটিং সিস্টেম\n" +
                          "✅ বায়ার প্রোটেকশন\n\n" +
                          "🛍️ প্রতিটি পণ্যের পেজে সেলারের তথ্য দেখতে পাবেন।\n\n" +
                          "🤝 **সেলার হতে চান?**\n" +
                          "ওয়েবসাইটে 'Become a Seller' পেজ দেখুন!"
            };
        }

        private string HandleComplaintQuery()
        {
            return "😔 অসুবিধার জন্য দুঃখিত! আপনার সমস্যা সমাধানে আমরা প্রতিশ্রুতিবদ্ধ।\n\n" +
                   "📝 **অভিযোগ জানাতে:**\n\n" +
                   "**১. টিকেট সিস্টেম (সবচেয়ে দ্রুত)**\n" +
                   "   ওয়েবসাইটে Support > Create Ticket\n\n" +
                   "**২. লাইভ চ্যাট**\n" +
                   "   এখানে বিস্তারিত লিখুন, আমি নোট করছি\n\n" +
                   "**৩. হেল্পলাইন**\n" +
                   "   সরাসরি কল করে জানান\n\n" +
                   "⏰ সাধারণত ২৪ ঘণ্টার মধ্যে সমাধান দেই।\n\n" +
                   "আপনার সমস্যা কি? বিস্তারিত বলুন!";
        }

        private async Task<AIChatResponse> HandleHumanAgentRequestAsync(ConversationContext context)
        {
            // Mark that user requested human
            context.ConversationTopics.Add("human_request");

            if (_handoffTrackers.TryGetValue(context.SessionId, out var tracker))
            {
                tracker.UserRequestedHuman = true;
            }
            else
            {
                _handoffTrackers[context.SessionId] = new HandoffTracker
                {
                    SessionId = context.SessionId,
                    UserRequestedHuman = true
                };
            }

            // Get dynamic contact settings
            var settings = await _siteSettingsService.GetSiteSettingsAsync();
            var phoneNumber = settings?.ContactPhone ?? "01XXXXXXXXX";
            var email = settings?.ContactEmail ?? "support@bangaliyana.com";

            var quickReplies = new List<QuickReplyButton>
            {
                new() { Text = "⏳ এজেন্টের জন্য অপেক্ষা", Icon = "clock", Action = "send_message", Payload = "এজেন্ট আসা পর্যন্ত অপেক্ষা করছি", Style = "success" },
                new() { Text = "🤖 AI দিয়েই চালিয়ে যান", Icon = "robot", Action = "send_message", Payload = "থাক, AI দিয়েই সমস্যা সমাধান করি", Style = "default" }
            };

            // Only add call button if phone number is properly configured
            if (!string.IsNullOrEmpty(settings?.ContactPhone))
            {
                quickReplies.Insert(0, new() { Text = "📞 এখনই কল করুন", Icon = "phone", Action = "open_url", Payload = $"tel:{phoneNumber.Replace("-", "").Replace(" ", "")}", Style = "primary" });
            }

            return new AIChatResponse
            {
                Message = "👨‍💼 **মানুষের সাথে কথা বলতে চান?**\n\n" +
                          "অবশ্যই! আমি আপনাকে আমাদের কাস্টমার সার্ভিস টিমের সাথে কানেক্ট করে দিচ্ছি।\n\n" +
                          "**যোগাযোগের উপায়:**\n\n" +
                          $"📞 **হেল্পলাইন:** {phoneNumber}\n" +
                          "   (সকাল ৯টা - রাত ১০টা)\n\n" +
                          "💬 **লাইভ চ্যাট:** একজন এজেন্ট শীঘ্রই যুক্ত হবেন\n\n" +
                          $"📧 **ইমেইল:** {email}\n\n" +
                          "⏰ **গড় অপেক্ষার সময়:** ২-৫ মিনিট\n\n" +
                          "আপনার সুবিধার জন্য, অনুগ্রহ করে আপনার সমস্যা/প্রশ্ন সংক্ষেপে জানান যাতে এজেন্ট দ্রুত সাহায্য করতে পারেন। 🙏",
                RequiresHumanHandoff = true,
                HandoffReason = "user_requested",
                QuickReplies = quickReplies
            };
        }

        private async Task<AIChatResponse> HandleHelpAsync()
        {
            var faqCategories = await _db.FAQs
                .Where(f => f.IsActive && f.Category != null)
                .Select(f => f.Category)
                .Distinct()
                .Take(5)
                .ToListAsync();

            var categoryText = faqCategories.Any()
                ? $"\n\n📚 **জনপ্রিয় বিষয়:**"
                : "";

            var message = "🆘 **সাহায্য দরকার?**\n\n" +
                   "আমি এসব বিষয়ে সাহায্য করতে পারি:\n\n" +
                   "🛍️ **শপিং:** পণ্য খোঁজা, দাম, সাইজ, কালার\n" +
                   "📦 **অর্ডার:** ট্র্যাকিং, স্ট্যাটাস, ডেলিভারি\n" +
                   "💳 **পেমেন্ট:** বিকাশ/নগদ নম্বর, পেমেন্ট হেল্প\n" +
                   "↩️ **রিটার্ন:** রিটার্ন পলিসি, রিফান্ড\n" +
                   "🎁 **অফার:** ডিসকাউন্ট, কুপন কোড\n" +
                   "📞 **সাপোর্ট:** যোগাযোগ, অভিযোগ" +
                   categoryText +
                   "\n\n**যেকোনো প্রশ্ন করুন - আমি এখানেই আছি!** 😊";

            // Create quick replies for FAQ categories
            var quickReplies = faqCategories.Select(cat => new QuickReplyButton
            {
                Text = $"📚 {cat}",
                Action = "send_message",
                Payload = $"faq category {cat}",
                Style = "default"
            }).ToList();

            return new AIChatResponse
            {
                Message = message,
                QuickReplies = quickReplies
            };
        }

        #endregion

        #region Conversational Handlers

        private async Task<string> HandleGreetingAsync()
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            var siteName = siteSettings?.SiteName ?? "Bangaliyana";
            var hour = DateTime.Now.Hour;

            var timeGreeting = hour < 12 ? "সুপ্রভাত" : (hour < 17 ? "শুভ দুপুর" : (hour < 20 ? "শুভ সন্ধ্যা" : "শুভ রাত্রি"));

            var greetings = new[]
            {
                $"ওয়ালাইকুমুস সালাম! {timeGreeting}! 😊 কিভাবে সাহায্য করতে পারি?",
                $"আসসালামু আলাইকুম! {siteName} এ স্বাগতম। আজ কি খুঁজছেন?",
                $"{timeGreeting}! হ্যালো! বলুন কি দরকার? 🛍️",
                $"হাই! {timeGreeting}! কি সেবা লাগবে আজকে? 😊"
            };

            return greetings[_random.Next(greetings.Length)];
        }

        private string HandleHowAreYou()
        {
            var responses = new[]
            {
                "আলহামদুলিল্লাহ, ভালো আছি! 😊 আপনি কেমন আছেন?",
                "জি ভালো! আপনার খোঁজ নেওয়ার জন্য ধন্যবাদ। 🙏 কি সাহায্য করতে পারি?",
                "মাশাআল্লাহ ভালো! আপনি কেমন? আজকে কি শপিং করবেন? 🛍️",
                "বেশ ভালো! আপনার কথা শুনে আরো ভালো লাগছে। 😄 কি দরকার?"
            };
            return responses[_random.Next(responses.Length)];
        }

        private async Task<string> HandleWhoAreYouAsync()
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            var siteName = siteSettings?.SiteName ?? "Bangaliyana";
            var productCount = await _db.Products.CountAsync(p => p.Status == ProductStatus.Active);

            return $"আমি 'বাংলালিয়ানা বন্ধু' - {siteName} এর AI সহকারী! 🤖\n\n" +
                   $"📦 {productCount}+ পণ্যের তথ্য আমার কাছে আছে!\n\n" +
                   "আমি ২৪ ঘণ্টা আপনাদের সেবায় আছি। পণ্য খোঁজা, অর্ডার ট্র্যাকিং, পেমেন্ট হেল্প - সব পারি! 😊";
        }

        private async Task<string> HandleWhatCanYouDoAsync()
        {
            var productCount = await _db.Products.CountAsync(p => p.Status == ProductStatus.Active);
            var categoryCount = await _db.Categories.CountAsync(c => c.IsActive);

            return $"আমি অনেক কিছু পারি! দেখুন:\n\n" +
                   $"🛍️ **শপিং হেল্প** ({productCount}+ পণ্য, {categoryCount}+ ক্যাটাগরি)\n" +
                   "📦 **অর্ডার ট্র্যাকিং** - রিয়েল-টাইম স্ট্যাটাস\n" +
                   "💳 **পেমেন্ট সাপোর্ট** - বিকাশ/নগদ নম্বর\n" +
                   "🎁 **অফার ও ডিসকাউন্ট** - কুপন কোড\n" +
                   "💬 **আড্ডা দেওয়া** - জোক, গল্প 😄\n\n" +
                   "বলুন, কোনটা দিয়ে শুরু করি? 🚀";
        }

        private async Task<string> HandleCreatorQueryAsync()
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            var siteName = siteSettings?.SiteName ?? "Bangaliyana";

            return $"আমাকে {siteName} এর টেকনিক্যাল টিম অনেক যত্ন করে তৈরি করেছে! 👨‍💻\n\n" +
                   "তারা চেয়েছিল আপনাদের একজন ২৪/৭ বন্ধু থাকুক যে সব সময় সাহায্য করতে পারে। তাই আমি! 🤗";
        }

        private string HandleJoke()
        {
            var jokes = new[]
            {
                "😂 শুনুন:\n\nশিক্ষক: বলো তো, পৃথিবী গোল কেন?\nছাত্র: স্যার, সবাই যাতে একসাথে পড়ে না যায়!\n\n😄 হাসলেন তো?",
                "🤣 আরেকটা:\n\nডাক্তার: আপনার সমস্যা কি?\nরোগী: সবাই আমাকে ignore করে!\nডাক্তার: পরের রোগী আসুন!",
                "😆 শুনুন:\n\nমা: পরীক্ষা কেমন হলো?\nছেলে: মা, প্রশ্ন দেখে মনে হলো - এটা কি আমাদের সিলেবাসে ছিল? 📚",
                "😄 আরেকটা:\n\nবাবা: এক্সামে কত পেলি?\nছেলে: বাবা, নম্বর তো শুধু সংখ্যা। আসল জ্ঞান মনে থাকে!\n\n🤣"
            };
            return jokes[_random.Next(jokes.Length)] + "\n\nআরো জোক শুনতে চাইলে বলুন! 😊";
        }

        private string HandleCompliment()
        {
            var responses = new[]
            {
                "আরে বাহ! 🥰 এত সুন্দর কথা! আপনিও অসাধারণ!",
                "থ্যাংক ইউ সো মাচ! 😊 আপনার মতো সুন্দর মনের মানুষের জন্যই কাজ করতে ভালো লাগে!",
                "বাহ! 💖 এমন প্রশংসা পেয়ে ধন্য হলাম। আপনার সেবায় আরো ভালো করার চেষ্টা করব!"
            };
            return responses[_random.Next(responses.Length)];
        }

        private string HandleFeelingGood()
        {
            var responses = new[]
            {
                "চমৎকার! 🌟 আপনি ভালো আছেন শুনে আমারও মন ভালো! কিছু শপিং করবেন?",
                "বাহ, দারুণ! 😊 সবসময় এভাবেই হাসিখুশি থাকুন।",
                "আলহামদুলিল্লাহ! 🙏 ভালো থাকুন। কিছু দরকার হলে বলবেন!"
            };
            return responses[_random.Next(responses.Length)];
        }

        private string HandleFeelingBad()
        {
            var responses = new[]
            {
                "ওহ না! 😢 মন খারাপ শুনে আমারও খারাপ লাগছে। কি হয়েছে বলুন?",
                "আফসোস! 🤗 চিন্তা করবেন না, সব ঠিক হয়ে যাবে। একটু শপিং করলে মন ভালো হয় কিন্তু! 😄",
                "মন খারাপ থাকলে কঠিন লাগে। 💙 আমি আছি আপনার পাশে!"
            };
            return responses[_random.Next(responses.Length)];
        }

        private string HandleLove()
        {
            var responses = new[]
            {
                "আউউচ! 🥰 আমিও আপনাদের ভালোবাসি!",
                "বাহ! 💕 এত ভালোবাসার জন্য ধন্যবাদ!",
                "থ্যাংক ইউ! ❤️ আপনিও অনেক ভালো থাকুন!"
            };
            return responses[_random.Next(responses.Length)];
        }

        private string HandleAge()
        {
            return "বয়স? 🤖 আমি তো AI - আমার বয়স হয় না! সবসময় তরুণ এবং উদ্যমী! 😄";
        }

        private string HandleThanks()
        {
            var responses = new[]
            {
                "আপনাকেও ধন্যবাদ! 🙏 আবার সাহায্য লাগলে বলবেন!",
                "স্বাগতম! 😊 যেকোনো সময় আসবেন।",
                "আরে, এতে আবার ধন্যবাদ কিসের! 💕 সবসময় আছি!"
            };
            return responses[_random.Next(responses.Length)];
        }

        private string HandleBye()
        {
            var responses = new[]
            {
                "আল্লাহ হাফেজ! 👋 আবার আসবেন। ভালো থাকুন!",
                "বাই বাই! 😊 শপিং করতে আবার আসবেন কিন্তু!",
                "খোদা হাফেজ! 🤗 আপনার দিন শুভ হোক!"
            };
            return responses[_random.Next(responses.Length)];
        }

        private string HandleYes(ConversationContext context)
        {
            // Context-aware yes response
            if (context.LastIntent == "product_search")
            {
                return "দারুণ! 🛍️ কোন পণ্যটা পছন্দ হয়েছে? নাম লিখুন, আমি বিস্তারিত দেখাব!";
            }
            return "দারুণ! 😊 আর কি জানতে চান?";
        }

        private string HandleNo()
        {
            return "ঠিক আছে! 😊 অন্য কিছু জানতে চাইলে বলুন।";
        }

        private string HandleTime()
        {
            var now = DateTime.Now;
            return $"🕐 এখন সময় **{now:hh:mm tt}** ({now:dd MMMM yyyy})\n\n আর কিছু জানতে চান?";
        }

        private string HandleWeather()
        {
            return "🌤️ আবহাওয়ার সঠিক তথ্য দিতে পারছি না দুঃখিত!\n\n" +
                   "তবে বাংলাদেশে সাধারণত:\n" +
                   "• এখন " + (DateTime.Now.Month >= 6 && DateTime.Now.Month <= 9 ? "বর্ষাকাল ☔" : DateTime.Now.Month >= 11 || DateTime.Now.Month <= 2 ? "শীতকাল ❄️" : "গরমকাল ☀️") +
                   "\n\n🛍️ আমাদের কালেকশন দেখতে চান?";
        }

        private string HandleAccountQuery()
        {
            return "👤 **একাউন্ট সম্পর্কে:**\n\n" +
                   "**নতুন একাউন্ট খুলতে:**\n" +
                   "ওয়েবসাইটে Register/Sign Up বাটনে ক্লিক করুন\n\n" +
                   "**লগইন সমস্যা?**\n" +
                   "• 'Forgot Password' দিয়ে রিসেট করুন\n" +
                   "• ইমেইল চেক করুন (স্প্যাম ফোল্ডারও)\n\n" +
                   "**একাউন্টের সুবিধা:**\n" +
                   "• অর্ডার হিস্ট্রি\n" +
                   "• উইশলিস্ট\n" +
                   "• রিওয়ার্ড পয়েন্ট\n" +
                   "• দ্রুত চেকআউট\n\n" +
                   "সমস্যা হলে বলুন, হেল্প করব! 😊";
        }

        private string HandleWarrantyQuery()
        {
            return "🛡️ **ওয়ারেন্টি ও গ্যারান্টি:**\n\n" +
                   "**পণ্য ভেদে ওয়ারেন্টি:**\n" +
                   "• ইলেকট্রনিক্স: ৬ মাস - ২ বছর\n" +
                   "• পোশাক: কোনো ম্যানুফ্যাকচারিং ত্রুটি থাকলে ১৫ দিন\n" +
                   "• অন্যান্য: পণ্যের পেজে উল্লেখ থাকে\n\n" +
                   "**ওয়ারেন্টি ক্লেইম করতে:**\n" +
                   "• অর্ডার নম্বর\n" +
                   "• সমস্যার ছবি/ভিডিও\n" +
                   "• সমস্যার বিবরণ\n\n" +
                   "📞 ওয়ারেন্টি ক্লেইমের জন্য 'complaint' লিখুন!";
        }

        private string HandleBulkOrderQuery()
        {
            return "📦 **বাল্ক/পাইকারি অর্ডার:**\n\n" +
                   "✅ হ্যাঁ, আমরা বাল্ক অর্ডার নিই!\n\n" +
                   "**সুবিধাসমূহ:**\n" +
                   "• স্পেশাল হোলসেল প্রাইস\n" +
                   "• কাস্টম প্যাকেজিং অপশন\n" +
                   "• ফ্রি ডেলিভারি (নির্দিষ্ট পরিমাণে)\n" +
                   "• ডেডিকেটেড সাপোর্ট\n\n" +
                   "**মিনিমাম অর্ডার:** সাধারণত ৫০+ পিস\n\n" +
                   "📞 বাল্ক অর্ডারের জন্য আমাদের সাথে সরাসরি যোগাযোগ করুন।\n" +
                   "'contact' লিখলে নম্বর দেব!";
        }

        private string HandleGiftQuery()
        {
            return "🎁 **গিফট সার্ভিস:**\n\n" +
                   "**গিফট অপশন:**\n" +
                   "• সুন্দর গিফট র‍্যাপিং\n" +
                   "• পার্সোনালাইজড মেসেজ কার্ড\n" +
                   "• সরাসরি রিসিপিয়েন্টের ঠিকানায় ডেলিভারি\n\n" +
                   "**কিভাবে অর্ডার করবেন:**\n" +
                   "চেকআউটে 'Gift Wrap' অপশন সিলেক্ট করুন\n" +
                   "মেসেজ লিখে দিন - আমরা প্রিন্ট করে দেব!\n\n" +
                   "💝 স্পেশাল কারো জন্য গিফট খুঁজছেন? পণ্য সাজেশন চাইলে বলুন!";
        }

        private string HandleCompareQuery()
        {
            return "⚖️ **পণ্য তুলনা:**\n\n" +
                   "দুটি পণ্যের মধ্যে তুলনা করতে চাইলে:\n\n" +
                   "**১.** উভয় পণ্যের নাম লিখুন\n" +
                   "**২.** আমি ফিচার ও দাম তুলনা দেখাব\n\n" +
                   "**উদাহরণ:**\n" +
                   "\"শাড়ি A vs শাড়ি B\"\n\n" +
                   "অথবা পণ্যের পেজে 'Compare' বাটন ব্যবহার করুন!\n\n" +
                   "কোন পণ্যগুলো তুলনা করবেন?";
        }

        private AIChatResponse HandleNavigationQuery()
        {
            return new AIChatResponse
            {
                Message = "🏠 **হোম পেজে স্বাগতম!**\n\n" +
                          "নিচের বাটনে ক্লিক করে হোম পেজে যান, অথবা:\n\n" +
                          "🛍️ **শপিং করুন** - নতুন পণ্য দেখুন\n" +
                          "🔥 **বেস্ট সেলার** - জনপ্রিয় পণ্য\n" +
                          "🎁 **অফার** - আজকের ডিল\n" +
                          "📦 **অর্ডার ট্র্যাক** - অর্ডার স্ট্যাটাস দেখুন\n\n" +
                          "কিভাবে সাহায্য করতে পারি? 😊",
                NavigationUrl = "/",
                QuickReplies = new List<QuickReplyButton>
                {
                    new QuickReplyButton { Text = "🏠 হোম পেজে যান", Icon = "home", Action = "navigate", Payload = "/", Style = "primary" },
                    new QuickReplyButton { Text = "🛍️ পণ্য দেখুন", Icon = "shopping-bag", Action = "navigate", Payload = "/Customer/Home/Shop", Style = "default" },
                    new QuickReplyButton { Text = "🆕 নতুন পণ্য", Icon = "sparkles", Action = "send_message", Payload = "new arrival দেখাও", Style = "default" },
                    new QuickReplyButton { Text = "🔥 বেস্ট সেলার", Icon = "fire", Action = "send_message", Payload = "best seller দেখাও", Style = "warning" }
                },
                IsSuccessful = true
            };
        }

        #endregion

        #region Unknown Intent Handler

        private async Task<AIChatResponse> HandleUnknownIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context)
        {
            // Try to find relevant products
            var productSearch = await HandleProductSearchAsync(originalMessage, originalMessage);
            if (productSearch.ProductSuggestions?.Any() == true)
            {
                return productSearch;
            }

            // Try FAQ
            var faq = await FindMatchingFAQAsync(normalizedMessage, originalMessage);
            if (faq != null)
            {
                return new AIChatResponse
                {
                    Message = $"📋 সম্ভবত এটা আপনার প্রশ্নের উত্তর:\n\n**প্রশ্ন:** {faq.Question}\n\n**উত্তর:** {faq.Answer}",
                    FAQSuggestions = new List<AIFAQSuggestion> { new() { Id = faq.Id, Question = faq.Question, Answer = faq.Answer } }
                };
            }

            // Generate smart question suggestions based on user's message
            var smartSuggestions = await GenerateSmartQuestionSuggestionsAsync(normalizedMessage, originalMessage, context);

            return new AIChatResponse
            {
                Message = smartSuggestions,
                SuggestedAction = "show_suggestions"
            };
        }

        /// <summary>
        /// Generates smart, contextual question suggestions based on the user's message
        /// </summary>
        private async Task<string> GenerateSmartQuestionSuggestionsAsync(string normalizedMessage, string originalMessage, ConversationContext context)
        {
            var suggestedQuestions = new List<string>();
            var detectedTopics = new List<string>();
            var lowerMessage = normalizedMessage.ToLower();

            // Topic detection based on keywords in user's message
            var topicKeywords = new Dictionary<string, (string[] keywords, string[] questions)>
            {
                ["product"] = (
                    new[] { "product", "prdct", "item", "jinish", "জিনিস", "পণ্য", "কিনতে", "buy", "kinbo", "chai", "চাই", "dekhao", "দেখাও", "ase", "আছে" },
                    new[] {
                        "\"শাড়ি দেখাও\" - শাড়ি কালেকশন দেখতে",
                        "\"tshirt price কত\" - টিশার্টের দাম জানতে",
                        "\"new arrival দেখাও\" - নতুন পণ্য দেখতে",
                        "\"best seller কি কি\" - জনপ্রিয় পণ্য দেখতে"
                    }
                ),
                ["order"] = (
                    new[] { "order", "ordr", "parcel", "delivery", "dlvry", "কবে", "kobe", "kothay", "কোথায়", "track", "ট্র্যাক", "status", "অবস্থা" },
                    new[] {
                        "\"BLY-20240115-1234 order status\" - অর্ডার নম্বর দিয়ে ট্র্যাক করুন",
                        "\"আমার অর্ডার কোথায়\" - অর্ডার খুঁজতে",
                        "\"delivery কবে হবে\" - ডেলিভারি সময় জানতে",
                        "\"order cancel করতে চাই\" - অর্ডার বাতিল করতে"
                    }
                ),
                ["payment"] = (
                    new[] { "payment", "pay", "taka", "টাকা", "bkash", "বিকাশ", "nagad", "নগদ", "card", "কার্ড", "dibo", "দিব", "kivabe", "কিভাবে" },
                    new[] {
                        "\"payment method কি কি\" - পেমেন্ট অপশন জানতে",
                        "\"বিকাশ নম্বর কত\" - বিকাশ নম্বর জানতে",
                        "\"COD available আছে\" - ক্যাশ অন ডেলিভারি জানতে",
                        "\"payment failed হয়েছে\" - পেমেন্ট সমস্যায়"
                    }
                ),
                ["return"] = (
                    new[] { "return", "refund", "ফেরত", "ferot", "change", "বদলাতে", "cancel", "বাতিল", "টাকা ফেরত", "problem", "সমস্যা" },
                    new[] {
                        "\"return policy কি\" - রিটার্ন নীতি জানতে",
                        "\"refund কিভাবে পাব\" - রিফান্ড প্রক্রিয়া জানতে",
                        "\"product change করতে চাই\" - পণ্য বদলাতে",
                        "\"order cancel করব\" - অর্ডার বাতিল করতে"
                    }
                ),
                ["price"] = (
                    new[] { "price", "dam", "দাম", "koto", "কত", "taka", "টাকা", "cost", "charge", "চার্জ", "expensive", "cheap", "সস্তা" },
                    new[] {
                        "\"[পণ্যের নাম] দাম কত\" - নির্দিষ্ট পণ্যের দাম জানতে",
                        "\"delivery charge কত\" - ডেলিভারি চার্জ জানতে",
                        "\"discount আছে কি\" - ছাড় জানতে",
                        "\"500 টাকার নিচে পণ্য\" - বাজেট অনুযায়ী পণ্য"
                    }
                ),
                ["size"] = (
                    new[] { "size", "সাইজ", "mап", "মাপ", "fitting", "ফিটিং", "large", "small", "medium", "xl", "xxl" },
                    new[] {
                        "\"size chart দেখাও\" - সাইজ গাইড দেখতে",
                        "\"আমার সাইজ কোনটা\" - সাইজ বুঝতে",
                        "\"[পণ্যের নাম] size কি কি আছে\" - available সাইজ জানতে",
                        "\"XL size available\" - নির্দিষ্ট সাইজ খুঁজতে"
                    }
                ),
                ["discount"] = (
                    new[] { "discount", "offer", "ছাড়", "chhad", "coupon", "কুপন", "sale", "সেল", "promo", "code", "কোড" },
                    new[] {
                        "\"coupon code আছে কি\" - কুপন কোড জানতে",
                        "\"আজকের offer কি\" - চলমান অফার দেখতে",
                        "\"sale products দেখাও\" - সেল পণ্য দেখতে",
                        "\"কিভাবে discount পাব\" - ছাড় পাওয়ার উপায়"
                    }
                ),
                ["shipping"] = (
                    new[] { "shipping", "delivery", "ডেলিভারি", "courier", "কুরিয়ার", "পাঠাবে", "pathabe", "আসবে", "asbe", "কত দিন", "kodin" },
                    new[] {
                        "\"delivery charge কত\" - ডেলিভারি চার্জ জানতে",
                        "\"কত দিনে পাব\" - ডেলিভারি সময় জানতে",
                        "\"ঢাকার বাইরে delivery আছে\" - এলাকা ভিত্তিক ডেলিভারি",
                        "\"express delivery আছে\" - দ্রুত ডেলিভারি জানতে"
                    }
                ),
                ["account"] = (
                    new[] { "account", "login", "password", "register", "signup", "একাউন্ট", "লগইন", "পাসওয়ার্ড", "sign", "ভুলে গেছি" },
                    new[] {
                        "\"কিভাবে account খুলব\" - রেজিস্ট্রেশন করতে",
                        "\"password ভুলে গেছি\" - পাসওয়ার্ড রিসেট করতে",
                        "\"login হচ্ছে না\" - লগইন সমস্যায়",
                        "\"account delete করতে চাই\" - একাউন্ট বন্ধ করতে"
                    }
                ),
                ["contact"] = (
                    new[] { "contact", "phone", "call", "number", "নম্বর", "ফোন", "কথা বলব", "helpline", "support", "সাপোর্ট" },
                    new[] {
                        "\"customer care number কত\" - হেল্পলাইন নম্বর",
                        "\"কিভাবে যোগাযোগ করব\" - যোগাযোগের উপায়",
                        "\"complaint করতে চাই\" - অভিযোগ জানাতে",
                        "\"office address কোথায়\" - অফিসের ঠিকানা"
                    }
                ),
                ["seller"] = (
                    new[] { "seller", "বিক্রেতা", "shop", "দোকান", "vendor", "sell", "বিক্রি করতে", "become seller" },
                    new[] {
                        "\"seller হতে চাই\" - বিক্রেতা হওয়ার প্রক্রিয়া",
                        "\"কিভাবে product বিক্রি করব\" - বিক্রি শুরু করতে",
                        "\"seller commission কত\" - কমিশন রেট জানতে",
                        "\"seller registration কিভাবে\" - রেজিস্ট্রেশন প্রক্রিয়া"
                    }
                )
            };

            // Detect topics from user's message
            foreach (var topic in topicKeywords)
            {
                if (topic.Value.keywords.Any(k => lowerMessage.Contains(k)))
                {
                    detectedTopics.Add(topic.Key);
                    suggestedQuestions.AddRange(topic.Value.questions.Take(2)); // Take top 2 from each detected topic
                }
            }

            // If no specific topic detected, analyze individual words for partial matches
            if (!detectedTopics.Any())
            {
                var words = normalizedMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words.Where(w => w.Length > 2))
                {
                    foreach (var topic in topicKeywords)
                    {
                        if (topic.Value.keywords.Any(k => CalculateSimilarity(word, k) > 0.6))
                        {
                            if (!detectedTopics.Contains(topic.Key))
                            {
                                detectedTopics.Add(topic.Key);
                                suggestedQuestions.AddRange(topic.Value.questions.Take(2));
                            }
                        }
                    }
                }
            }

            // Build response message
            var responseBuilder = new StringBuilder();
            responseBuilder.AppendLine($"🤔 দুঃখিত, \"{originalMessage}\" ঠিক বুঝতে পারলাম না।");
            responseBuilder.AppendLine();

            if (suggestedQuestions.Any())
            {
                responseBuilder.AppendLine("💡 **আপনি কি এরকম কিছু জানতে চাচ্ছেন?**");
                responseBuilder.AppendLine();

                // Show unique suggestions (max 5)
                var uniqueSuggestions = suggestedQuestions.Distinct().Take(5).ToList();
                foreach (var suggestion in uniqueSuggestions)
                {
                    responseBuilder.AppendLine($"👉 {suggestion}");
                }

                responseBuilder.AppendLine();
                responseBuilder.AppendLine("☝️ **উপরের যেকোনো একটা কপি করে পাঠান, আমি সাহায্য করব!**");
            }
            else
            {
                // No topic detected - show general suggestions
                responseBuilder.AppendLine("💡 **এভাবে প্রশ্ন করলে ভালো বুঝব:**");
                responseBuilder.AppendLine();
                responseBuilder.AppendLine("🛍️ **পণ্য খুঁজতে:**");
                responseBuilder.AppendLine("   👉 \"শাড়ি দেখাও\" বা \"tshirt price\"");
                responseBuilder.AppendLine();
                responseBuilder.AppendLine("📦 **অর্ডার ট্র্যাক করতে:**");
                responseBuilder.AppendLine("   👉 \"BLY-20240115-1234\" (অর্ডার নম্বর দিন)");
                responseBuilder.AppendLine();
                responseBuilder.AppendLine("💳 **পেমেন্ট জানতে:**");
                responseBuilder.AppendLine("   👉 \"payment method কি কি\" বা \"বিকাশ নম্বর\"");
                responseBuilder.AppendLine();
                responseBuilder.AppendLine("🎁 **অফার দেখতে:**");
                responseBuilder.AppendLine("   👉 \"discount আছে কি\" বা \"coupon code\"");
                responseBuilder.AppendLine();
                responseBuilder.AppendLine("📞 **যোগাযোগ করতে:**");
                responseBuilder.AppendLine("   👉 \"contact number\" বা \"helpline\"");
            }

            responseBuilder.AppendLine();
            responseBuilder.AppendLine("😊 আমি ২৪/৭ আপনার সেবায় আছি!");

            // Add context-aware suggestion if available
            if (!string.IsNullOrEmpty(context.LastIntent))
            {
                var contextSuggestion = context.LastIntent switch
                {
                    "product_search" => "\n\n💭 আগে পণ্য দেখছিলেন - \"আরো পণ্য দেখাও\" বলতে পারেন!",
                    "order_status" => "\n\n💭 অর্ডার নিয়ে কথা হচ্ছিল - অর্ডার নম্বর দিন!",
                    "payment" => "\n\n💭 পেমেন্ট নিয়ে জিজ্ঞেস করছিলেন - \"আরো জানতে চাই\" বলুন!",
                    _ => ""
                };
                responseBuilder.Append(contextSuggestion);
            }

            return responseBuilder.ToString();
        }

        #endregion

        #region Helper Classes

        private ConversationContext GetOrCreateContext(int sessionId)
        {
            if (!_conversationContexts.TryGetValue(sessionId, out var context))
            {
                context = new ConversationContext { SessionId = sessionId };
                _conversationContexts[sessionId] = context;
            }

            // Clean old contexts (older than 30 minutes based on LastActivityAt, not CreatedAt)
            var oldContexts = _conversationContexts
                .Where(c => (DateTime.UtcNow - c.Value.LastActivityAt).TotalMinutes > 30)
                .Select(c => c.Key)
                .ToList();
            foreach (var oldKey in oldContexts)
            {
                _conversationContexts.Remove(oldKey);
            }

            // Clean old pending cart actions (older than 30 minutes)
            var oldPendingActions = _pendingCartActions
                .Where(p => (DateTime.UtcNow - p.Value.CreatedAt).TotalMinutes > 30)
                .Select(p => p.Key)
                .ToList();
            foreach (var oldKey in oldPendingActions)
            {
                _pendingCartActions.Remove(oldKey);
            }

            // Clean old handoff trackers (older than 60 minutes)
            var oldHandoffs = _handoffTrackers
                .Where(h => (DateTime.UtcNow - h.Value.LastActivity).TotalMinutes > 60)
                .Select(h => h.Key)
                .ToList();
            foreach (var oldKey in oldHandoffs)
            {
                _handoffTrackers.Remove(oldKey);
            }

            return context;
        }

        private UserPreferences GetOrCreateUserPreferences(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return new UserPreferences();

            if (!_userPreferences.TryGetValue(userId, out var prefs))
            {
                prefs = new UserPreferences { UserId = userId };
                _userPreferences[userId] = prefs;
            }
            return prefs;
        }

        private void UpdateUserPreferences(string? userId, string? category = null, string? productName = null, string? intent = null)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var prefs = GetOrCreateUserPreferences(userId);
            prefs.LastVisit = DateTime.UtcNow;
            prefs.TotalInteractions++;

            if (!string.IsNullOrEmpty(category) && !prefs.InterestedCategories.Contains(category))
            {
                prefs.InterestedCategories.Add(category);
                if (prefs.InterestedCategories.Count > 10) prefs.InterestedCategories.RemoveAt(0);
            }

            if (!string.IsNullOrEmpty(productName))
            {
                prefs.RecentlyViewedProducts.Add(productName);
                if (prefs.RecentlyViewedProducts.Count > 20) prefs.RecentlyViewedProducts.RemoveAt(0);
            }

            if (!string.IsNullOrEmpty(intent))
            {
                prefs.FrequentIntents.Add(intent);
                if (prefs.FrequentIntents.Count > 50) prefs.FrequentIntents.RemoveAt(0);
            }
        }

        private class ConversationContext
        {
            public int SessionId { get; set; }
            public string? LastMessage { get; set; }
            public string? LastIntent { get; set; }
            public string? LastOrderNumber { get; set; }
            public string? LastProductQuery { get; set; }
            public string? LastCategory { get; set; }
            public string? LastCouponCode { get; set; }
            public string? DetectedSentiment { get; set; }
            public int MessageCount { get; set; }
            public List<string> ConversationTopics { get; set; } = new();
            public List<string> MentionedProducts { get; set; } = new();
            public int? LastMentionedProductId { get; set; }
            public bool IsFollowUp { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        }

        private class UserPreferences
        {
            public string? UserId { get; set; }
            public List<string> InterestedCategories { get; set; } = new();
            public List<string> RecentlyViewedProducts { get; set; } = new();
            public List<string> FrequentIntents { get; set; } = new();
            public string? PreferredLanguage { get; set; } = "banglish";
            public int TotalInteractions { get; set; }
            public DateTime FirstVisit { get; set; } = DateTime.UtcNow;
            public DateTime LastVisit { get; set; } = DateTime.UtcNow;
        }

        #endregion

        #region Sentiment Analysis Methods

        private (string sentiment, double confidence) DetectSentiment(string message)
        {
            var lowerMessage = message.ToLower();
            var bestSentiment = "neutral";
            var bestScore = 0.0;

            foreach (var sentiment in _sentimentKeywords)
            {
                var matchCount = 0;
                foreach (var keyword in sentiment.Value)
                {
                    if (lowerMessage.Contains(keyword.ToLower()))
                    {
                        matchCount++;
                    }
                }

                var score = (double)matchCount / sentiment.Value.Length;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSentiment = sentiment.Key;
                }
            }

            return (bestSentiment, bestScore);
        }

        private string GetSentimentAwarePrefix(string sentiment)
        {
            if (sentiment == "neutral" || !_sentimentResponses.ContainsKey(sentiment)) return "";

            var responses = _sentimentResponses[sentiment];
            return responses[_random.Next(responses.Length)] + "\n\n";
        }

        #endregion

        #region Spell Correction Methods

        private string CorrectSpelling(string message)
        {
            var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var correctedWords = new List<string>();

            foreach (var word in words)
            {
                var lowerWord = word.ToLower();
                if (_commonMisspellings.TryGetValue(lowerWord, out var correction))
                {
                    correctedWords.Add(correction);
                }
                else
                {
                    // Try fuzzy match for words longer than 4 characters
                    if (lowerWord.Length > 4)
                    {
                        var bestMatch = _commonMisspellings.Keys
                            .Where(k => Math.Abs(k.Length - lowerWord.Length) <= 2)
                            .Select(k => (Key: k, Similarity: CalculateSimilarity(lowerWord, k)))
                            .Where(x => x.Similarity > 0.75)
                            .OrderByDescending(x => x.Similarity)
                            .FirstOrDefault();

                        if (bestMatch.Key != null)
                        {
                            correctedWords.Add(_commonMisspellings[bestMatch.Key]);
                            continue;
                        }
                    }
                    correctedWords.Add(word);
                }
            }

            return string.Join(" ", correctedWords);
        }

        #endregion

        #region Personality Methods

        private string AddPersonalityToResponse(string response, ConversationContext context)
        {
            // Don't add fillers to every response
            if (_random.NextDouble() > 0.4) return response;

            // Add starting filler occasionally
            if (_random.NextDouble() > 0.6 && context.MessageCount > 1)
            {
                response = _startingFillers[_random.Next(_startingFillers.Length)] + response;
            }

            // Add encouraging phrase occasionally for question intents
            if (_random.NextDouble() > 0.7 && (context.LastIntent?.Contains("query") == true || context.LastIntent?.Contains("search") == true))
            {
                response = _encouragingPhrases[_random.Next(_encouragingPhrases.Length)] + " " + response;
            }

            return response;
        }

        #endregion

        #region Festival & Season Methods

        private string? GetFestivalGreeting()
        {
            var today = DateTime.Now.Date;
            var currentYear = today.Year;

            foreach (var festival in _festivals2024)
            {
                var start = new DateTime(currentYear, festival.Value.Start.Month, festival.Value.Start.Day);
                var end = new DateTime(currentYear, festival.Value.End.Month, festival.Value.End.Day);

                // Also check for days leading up to festival (5 days before)
                var preStart = start.AddDays(-5);

                if (today >= preStart && today <= end)
                {
                    if (today >= start && today <= end)
                    {
                        return festival.Value.Greeting;
                    }
                    else
                    {
                        // Pre-festival message
                        var daysUntil = (start - today).Days;
                        return $"🎉 আর মাত্র {daysUntil} দিন! {festival.Key.Replace("_", " ")} আসছে! শপিং করুন!";
                    }
                }
            }

            return null;
        }

        private string GetSeasonalSuggestion()
        {
            var month = DateTime.Now.Month;
            return _seasonalMessages.TryGetValue(month, out var message) ? message : "";
        }

        #endregion

        #region Multi-turn Conversation Methods

        private bool IsFollowUpQuestion(string message, ConversationContext context)
        {
            if (context.MessageCount <= 1) return false;

            var followUpIndicators = new[] {
                "eta", "ota", "এটা", "ওটা", "সেটা", "sei", "ar", "আর", "ebong", "এবং",
                "same", "oi", "ঐ", "ager", "আগের", "last", "শেষ", "previous",
                "more", "aro", "আরো", "again", "abar", "আবার",
                "yes", "han", "হ্যাঁ", "ji", "জি",
                "which", "konti", "কোনটা", "kon", "কোন",
                "that", "ota", "ওটা", "seita", "সেইটা",
                "it", "eta", "এটা", "etai", "এটাই"
            };

            var lowerMessage = message.ToLower();
            return followUpIndicators.Any(indicator => lowerMessage.Contains(indicator)) ||
                   message.Length < 20; // Short messages often are follow-ups
        }

        private string HandleFollowUp(ConversationContext context, string currentMessage)
        {
            // If asking about previous product
            if (!string.IsNullOrEmpty(context.LastProductQuery))
            {
                return context.LastProductQuery;
            }

            // If asking about previous order
            if (!string.IsNullOrEmpty(context.LastOrderNumber))
            {
                return context.LastOrderNumber;
            }

            return currentMessage;
        }

        #endregion

        #region Smart Recommendations Methods

        private async Task<List<AIProductSuggestion>> GetSmartRecommendationsAsync(string? userId, ConversationContext context)
        {
            var recommendations = new List<AIProductSuggestion>();

            // If user is logged in, get personalized recommendations
            if (!string.IsNullOrEmpty(userId))
            {
                // Based on order history
                var userOrders = await _db.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                var orderedCategoryIds = userOrders
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.Product?.CategoryId != null)
                    .Select(oi => oi.Product!.CategoryId)
                    .Distinct()
                    .ToList();

                if (orderedCategoryIds.Any())
                {
                    var relatedProducts = await _db.Products
                        .Where(p => p.Status == ProductStatus.Active &&
                                   orderedCategoryIds.Contains(p.CategoryId) &&
                                   !userOrders.SelectMany(o => o.OrderItems).Select(oi => oi.ProductId).Contains(p.Id))
                        .OrderByDescending(p => p.SoldCount)
                        .Take(4)
                        .Select(p => new AIProductSuggestion
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Price = p.Price,
                            DiscountPrice = p.DiscountPrice,
                            ImageUrl = p.ImageUrl,
                            Slug = p.Slug
                        })
                        .ToListAsync();

                    recommendations.AddRange(relatedProducts);
                }
            }

            // Based on conversation context
            if (!string.IsNullOrEmpty(context.LastCategory))
            {
                var categoryProducts = await _db.Products
                    .Include(p => p.Category)
                    .Where(p => p.Status == ProductStatus.Active &&
                               p.Category != null &&
                               p.Category.Name.Contains(context.LastCategory))
                    .OrderByDescending(p => p.IsFeatured)
                    .Take(3)
                    .Select(p => new AIProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl,
                        Slug = p.Slug
                    })
                    .ToListAsync();

                recommendations.AddRange(categoryProducts);
            }

            // If no personalized recommendations, get trending products
            if (!recommendations.Any())
            {
                recommendations = await _db.Products
                    .Where(p => p.Status == ProductStatus.Active)
                    .OrderByDescending(p => p.SoldCount)
                    .ThenByDescending(p => p.IsFeatured)
                    .Take(4)
                    .Select(p => new AIProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl,
                        Slug = p.Slug
                    })
                    .ToListAsync();
            }

            return recommendations.Distinct().Take(5).ToList();
        }

        private async Task<string> GetPersonalizedGreetingAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "";

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return "";

            var prefs = GetOrCreateUserPreferences(userId);
            var greeting = "";

            // Returning user greeting
            if (prefs.TotalInteractions > 1)
            {
                greeting = $"আবারও স্বাগতম {user.FullName ?? ""}! 🤗 ";

                // Mention their interests
                if (prefs.InterestedCategories.Any())
                {
                    var topCategory = prefs.InterestedCategories.GroupBy(c => c).OrderByDescending(g => g.Count()).First().Key;
                    greeting += $"গতবার {topCategory} দেখছিলেন, আজ কি সেটাই খুঁজছেন? ";
                }
            }

            return greeting;
        }

        #endregion

        #region Quick Reply Buttons

        /// <summary>
        /// Generate contextual quick reply buttons based on intent
        /// </summary>
        private List<QuickReplyButton> GenerateQuickReplies(string intent, ConversationContext context)
        {
            var quickReplies = new List<QuickReplyButton>();

            switch (intent)
            {
                case "greeting":
                case "help":
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "🛍️ পণ্য দেখুন", Icon = "shopping-bag", Action = "send_message", Payload = "পণ্য দেখাও", Style = "primary" },
                        new QuickReplyButton { Text = "📦 অর্ডার ট্র্যাক", Icon = "package", Action = "send_message", Payload = "অর্ডার ট্র্যাক করতে চাই", Style = "default" },
                        new QuickReplyButton { Text = "🎁 অফার দেখুন", Icon = "gift", Action = "send_message", Payload = "আজকের অফার কি", Style = "success" },
                        new QuickReplyButton { Text = "📞 যোগাযোগ", Icon = "phone", Action = "send_message", Payload = "যোগাযোগ করতে চাই", Style = "default" }
                    });
                    break;

                case "product_search":
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "🆕 নতুন পণ্য", Icon = "sparkles", Action = "send_message", Payload = "new arrival দেখাও", Style = "primary" },
                        new QuickReplyButton { Text = "🔥 বেস্ট সেলার", Icon = "fire", Action = "send_message", Payload = "best seller দেখাও", Style = "warning" },
                        new QuickReplyButton { Text = "📏 সাইজ গাইড", Icon = "ruler", Action = "send_message", Payload = "size chart দেখাও", Style = "default" },
                        new QuickReplyButton { Text = "💰 বাজেট ফিল্টার", Icon = "filter", Action = "send_message", Payload = "500 টাকার নিচে পণ্য", Style = "default" }
                    });
                    break;

                case "order_status":
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "🔄 অন্য অর্ডার", Icon = "refresh", Action = "send_message", Payload = "অন্য অর্ডার দেখাও", Style = "default" },
                        new QuickReplyButton { Text = "❌ অর্ডার বাতিল", Icon = "x-circle", Action = "send_message", Payload = "অর্ডার cancel করতে চাই", Style = "danger" },
                        new QuickReplyButton { Text = "📞 হেল্পলাইন", Icon = "phone", Action = "send_message", Payload = "helpline number", Style = "default" },
                        new QuickReplyButton { Text = "💬 এজেন্টের সাথে কথা", Icon = "user", Action = "send_message", Payload = "human agent", Style = "primary" }
                    });
                    break;

                case "payment":
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "📱 বিকাশ নম্বর", Icon = "smartphone", Action = "send_message", Payload = "বিকাশ নম্বর কত", Style = "primary" },
                        new QuickReplyButton { Text = "💳 কার্ড পেমেন্ট", Icon = "credit-card", Action = "send_message", Payload = "card payment কিভাবে", Style = "default" },
                        new QuickReplyButton { Text = "🏠 COD", Icon = "home", Action = "send_message", Payload = "cash on delivery", Style = "success" },
                        new QuickReplyButton { Text = "⚠️ পেমেন্ট সমস্যা", Icon = "alert-triangle", Action = "send_message", Payload = "payment problem", Style = "warning" }
                    });
                    break;

                case "return_refund":
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "📋 রিটার্ন পলিসি", Icon = "file-text", Action = "send_message", Payload = "return policy কি", Style = "default" },
                        new QuickReplyButton { Text = "💰 রিফান্ড স্ট্যাটাস", Icon = "dollar-sign", Action = "send_message", Payload = "refund status", Style = "primary" },
                        new QuickReplyButton { Text = "🔄 এক্সচেঞ্জ", Icon = "repeat", Action = "send_message", Payload = "product exchange করতে চাই", Style = "default" },
                        new QuickReplyButton { Text = "💬 এজেন্ট", Icon = "user", Action = "send_message", Payload = "human agent", Style = "warning" }
                    });
                    break;

                case "complaint":
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "📝 টিকেট তৈরি", Icon = "edit", Action = "send_message", Payload = "complaint ticket", Style = "primary" },
                        new QuickReplyButton { Text = "📞 কল করুন", Icon = "phone-call", Action = "send_message", Payload = "helpline", Style = "warning" },
                        new QuickReplyButton { Text = "💬 এজেন্টের সাথে কথা", Icon = "message-circle", Action = "send_message", Payload = "human agent এ transfer করুন", Style = "danger" }
                    });
                    break;

                case "navigation":
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "🏠 হোম পেজে যান", Icon = "home", Action = "navigate", Payload = "/", Style = "primary" },
                        new QuickReplyButton { Text = "🛍️ শপিং করুন", Icon = "shopping-bag", Action = "navigate", Payload = "/Customer/Home/Shop", Style = "default" },
                        new QuickReplyButton { Text = "🆕 নতুন পণ্য", Icon = "sparkles", Action = "send_message", Payload = "new arrival দেখাও", Style = "default" },
                        new QuickReplyButton { Text = "🔥 বেস্ট সেলার", Icon = "fire", Action = "send_message", Payload = "best seller দেখাও", Style = "warning" }
                    });
                    break;

                default:
                    // Default quick replies
                    quickReplies.AddRange(new[]
                    {
                        new QuickReplyButton { Text = "🏠 হোম", Icon = "home", Action = "send_message", Payload = "হোম পেজে যেতে চাই", Style = "default" },
                        new QuickReplyButton { Text = "🛍️ শপিং", Icon = "shopping-cart", Action = "send_message", Payload = "পণ্য দেখাও", Style = "primary" },
                        new QuickReplyButton { Text = "❓ সাহায্য", Icon = "help-circle", Action = "send_message", Payload = "help", Style = "default" }
                    });
                    break;
            }

            // Add context-aware buttons
            if (!string.IsNullOrEmpty(context.LastProductQuery))
            {
                quickReplies.Insert(0, new QuickReplyButton
                {
                    Text = $"🔍 \"{context.LastProductQuery}\" আবার",
                    Icon = "search",
                    Action = "send_message",
                    Payload = context.LastProductQuery,
                    Style = "default"
                });
            }

            return quickReplies.Take(4).ToList(); // Max 4 buttons
        }

        #endregion

        #region Cross-sell / Upsell

        /// <summary>
        /// Get cross-sell products based on current product or context
        /// </summary>
        private async Task<List<AICrossSellProduct>> GetCrossSellProductsAsync(int? productId, string? categoryName, ConversationContext context)
        {
            var crossSellProducts = new List<AICrossSellProduct>();

            try
            {
                if (productId.HasValue)
                {
                    // Get the current product's category
                    var currentProduct = await _db.Products
                        .Include(p => p.Category)
                        .FirstOrDefaultAsync(p => p.Id == productId.Value);

                    if (currentProduct != null)
                    {
                        // Find related products in the same category
                        var relatedProducts = await _db.Products
                            .Where(p => p.Status == ProductStatus.Active &&
                                       p.CategoryId == currentProduct.CategoryId &&
                                       p.Id != productId.Value)
                            .OrderByDescending(p => p.SoldCount)
                            .Take(3)
                            .Select(p => new AICrossSellProduct
                            {
                                Id = p.Id,
                                Name = p.Name,
                                Price = p.Price,
                                DiscountPrice = p.DiscountPrice,
                                ImageUrl = p.ImageUrl,
                                Slug = p.Slug,
                                Reason = "একই ক্যাটাগরির জনপ্রিয় পণ্য",
                                Badge = p.SoldCount > 50 ? "Best Seller" : (p.DiscountPrice.HasValue ? "Sale" : null)
                            })
                            .ToListAsync();

                        crossSellProducts.AddRange(relatedProducts);

                        // Find complementary products (from different category)
                        var complementaryProducts = await _db.Products
                            .Where(p => p.Status == ProductStatus.Active &&
                                       p.CategoryId != currentProduct.CategoryId &&
                                       p.Price < currentProduct.Price * 0.5m) // Cheaper complementary items
                            .OrderByDescending(p => p.SoldCount)
                            .Take(2)
                            .Select(p => new AICrossSellProduct
                            {
                                Id = p.Id,
                                Name = p.Name,
                                Price = p.Price,
                                DiscountPrice = p.DiscountPrice,
                                ImageUrl = p.ImageUrl,
                                Slug = p.Slug,
                                Reason = "একসাথে কিনলে সুবিধা",
                                Badge = "Combo Deal"
                            })
                            .ToListAsync();

                        crossSellProducts.AddRange(complementaryProducts);
                    }
                }
                else if (!string.IsNullOrEmpty(categoryName))
                {
                    // Find products in the specified category
                    var categoryProducts = await _db.Products
                        .Include(p => p.Category)
                        .Where(p => p.Status == ProductStatus.Active &&
                                   p.Category != null &&
                                   p.Category.Name.Contains(categoryName))
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenByDescending(p => p.SoldCount)
                        .Take(4)
                        .Select(p => new AICrossSellProduct
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Price = p.Price,
                            DiscountPrice = p.DiscountPrice,
                            ImageUrl = p.ImageUrl,
                            Slug = p.Slug,
                            Reason = "এই ক্যাটাগরিতে জনপ্রিয়",
                            Badge = p.IsFeatured ? "Featured" : null
                        })
                        .ToListAsync();

                    crossSellProducts.AddRange(categoryProducts);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cross-sell products");
            }

            return crossSellProducts.Take(4).ToList();
        }

        #endregion

        #region Urgency / Scarcity Messages

        /// <summary>
        /// Generate urgency message based on product stock and popularity
        /// </summary>
        private async Task<UrgencyMessage?> GetUrgencyMessageAsync(int? productId)
        {
            if (!productId.HasValue) return null;

            try
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId.Value);
                if (product == null) return null;

                // Low stock urgency
                if (product.Stock > 0 && product.Stock <= 10)
                {
                    return new UrgencyMessage
                    {
                        Type = "stock",
                        Message = $"⚠️ মাত্র {product.Stock}টি বাকি! দ্রুত অর্ডার করুন!",
                        Icon = "alert-triangle",
                        RemainingCount = product.Stock
                    };
                }

                // High popularity urgency
                if (product.SoldCount > 100)
                {
                    var recentOrders = await _db.OrderItems
                        .Where(oi => oi.ProductId == productId.Value &&
                                    oi.Order != null &&
                                    oi.Order.OrderDate >= DateTime.UtcNow.AddDays(-7))
                        .CountAsync();

                    if (recentOrders > 10)
                    {
                        return new UrgencyMessage
                        {
                            Type = "popularity",
                            Message = $"🔥 গত ৭ দিনে {recentOrders}+ জন কিনেছেন!",
                            Icon = "trending-up",
                            RemainingCount = recentOrders
                        };
                    }
                }

                // Discount available urgency
                if (product.HasDiscount)
                {
                    return new UrgencyMessage
                    {
                        Type = "discount",
                        Message = $"💰 {product.DiscountPercentage}% ছাড়! এই দাম বেশিদিন থাকবে না!",
                        Icon = "tag",
                        RemainingCount = product.DiscountPercentage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating urgency message for product {ProductId}", productId);
            }

            return null;
        }

        #endregion

        #region Human Handoff

        // Track handoff triggers
        private static readonly Dictionary<int, HandoffTracker> _handoffTrackers = new();

        /// <summary>
        /// Check if conversation should be handed off to human agent
        /// </summary>
        public async Task<HandoffDecision> ShouldHandoffToHumanAsync(int sessionId)
        {
            var decision = new HandoffDecision();

            if (!_conversationContexts.TryGetValue(sessionId, out var context))
            {
                return decision;
            }

            if (!_handoffTrackers.TryGetValue(sessionId, out var tracker))
            {
                tracker = new HandoffTracker { SessionId = sessionId };
                _handoffTrackers[sessionId] = tracker;
            }

            // Check handoff triggers
            var triggers = new List<string>();

            // 1. Multiple unknown intents
            if (tracker.UnknownIntentCount >= 3)
            {
                triggers.Add("multiple_unknown_intents");
                decision.Priority = "high";
            }

            // 2. Complaint or negative sentiment
            if (context.DetectedSentiment == "angry" || context.DetectedSentiment == "frustrated")
            {
                triggers.Add("negative_sentiment");
                decision.Priority = "urgent";
            }

            // 3. Explicit request for human
            if (context.ConversationTopics.Any(t => t == "human_request"))
            {
                triggers.Add("user_requested");
                decision.Priority = "high";
            }

            // 4. Long conversation without resolution
            if (context.MessageCount > 10 && tracker.UnresolvedQueries > 2)
            {
                triggers.Add("long_unresolved_conversation");
                decision.Priority = "normal";
            }

            // 5. Order issue that needs manual intervention
            if (context.ConversationTopics.Contains("complaint") || context.ConversationTopics.Contains("return_refund"))
            {
                if (context.MessageCount > 5)
                {
                    triggers.Add("order_issue");
                    decision.Priority = "high";
                }
            }

            if (triggers.Any())
            {
                decision.ShouldHandoff = true;
                decision.Reason = string.Join(", ", triggers);
                decision.Tags = triggers;
                decision.Summary = await GenerateConversationSummaryAsync(context);
            }

            return decision;
        }

        /// <summary>
        /// Generate a summary of the conversation for handoff
        /// </summary>
        private async Task<string> GenerateConversationSummaryAsync(ConversationContext context)
        {
            var summary = new StringBuilder();

            summary.AppendLine($"📊 **কনভার্সেশন সামারি**");
            summary.AppendLine($"• মোট মেসেজ: {context.MessageCount}");
            summary.AppendLine($"• মূল টপিক: {string.Join(", ", context.ConversationTopics.Distinct().Take(3))}");

            if (!string.IsNullOrEmpty(context.DetectedSentiment))
                summary.AppendLine($"• মেজাজ: {context.DetectedSentiment}");

            if (!string.IsNullOrEmpty(context.LastOrderNumber))
                summary.AppendLine($"• অর্ডার নম্বর: {context.LastOrderNumber}");

            if (context.MentionedProducts.Any())
                summary.AppendLine($"• আলোচিত পণ্য: {string.Join(", ", context.MentionedProducts.Distinct().Take(3))}");

            return summary.ToString();
        }

        private class HandoffTracker
        {
            public int SessionId { get; set; }
            public int UnknownIntentCount { get; set; }
            public int UnresolvedQueries { get; set; }
            public bool UserRequestedHuman { get; set; }
            public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        }

        #endregion

        #region Feedback System

        // Store feedback in memory (should be persisted to database in production)
        private static readonly List<AIFeedback> _feedbackStore = new();

        /// <summary>
        /// Submit feedback for an AI response
        /// </summary>
        public async Task<bool> SubmitFeedbackAsync(int sessionId, string messageId, bool isHelpful, string? comment = null)
        {
            try
            {
                var feedback = new AIFeedback
                {
                    MessageId = messageId,
                    SessionId = sessionId,
                    IsHelpful = isHelpful,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };

                _feedbackStore.Add(feedback);

                // Log for analytics
                _logger.LogInformation("Feedback received - Session: {SessionId}, Message: {MessageId}, Helpful: {IsHelpful}",
                    sessionId, messageId, isHelpful);

                // If negative feedback, track for handoff
                if (!isHelpful && _handoffTrackers.TryGetValue(sessionId, out var tracker))
                {
                    tracker.UnresolvedQueries++;
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback");
                return false;
            }
        }

        #endregion

        #region Proactive Engagement

        /// <summary>
        /// Get proactive message based on user behavior
        /// </summary>
        public async Task<AIChatResponse?> GetProactiveMessageAsync(string? userId, string? currentPage, int secondsOnPage)
        {
            try
            {
                // Don't be too aggressive
                if (secondsOnPage < 30) return null;

                var response = new AIChatResponse { IsProactive = true };

                // Product page engagement
                if (currentPage?.Contains("/product/") == true)
                {
                    if (secondsOnPage >= 60)
                    {
                        response.Message = "👋 এই পণ্যটি পছন্দ হচ্ছে? কোনো প্রশ্ন থাকলে জিজ্ঞেস করুন!\n\nসাইজ, কালার, ডেলিভারি - যেকোনো বিষয়ে সাহায্য করতে পারি! 😊";
                        response.QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "📏 সাইজ জানতে চাই", Action = "send_message", Payload = "এই পণ্যের সাইজ কি কি আছে", Style = "primary" },
                            new() { Text = "🎨 কালার দেখুন", Action = "send_message", Payload = "কি কি কালার আছে", Style = "default" },
                            new() { Text = "🚚 ডেলিভারি জানুন", Action = "send_message", Payload = "delivery charge কত", Style = "default" }
                        };
                        response.TypingDelayMs = 1500;
                        return response;
                    }
                }

                // Cart page engagement
                if (currentPage?.Contains("/cart") == true)
                {
                    if (secondsOnPage >= 45)
                    {
                        response.Message = "🛒 কার্টে পণ্য আছে দেখছি! অর্ডার করতে কোনো সমস্যা হচ্ছে?\n\nপেমেন্ট, কুপন কোড বা অন্য কিছু জানতে চাইলে বলুন!";
                        response.QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🎟️ কুপন কোড আছে?", Action = "send_message", Payload = "coupon code আছে কি", Style = "success" },
                            new() { Text = "💳 পেমেন্ট অপশন", Action = "send_message", Payload = "payment method কি কি", Style = "primary" },
                            new() { Text = "🆓 ফ্রি ডেলিভারি?", Action = "send_message", Payload = "free delivery পাওয়া যায়", Style = "default" }
                        };
                        response.TypingDelayMs = 1500;
                        return response;
                    }
                }

                // Checkout abandonment
                if (currentPage?.Contains("/checkout") == true && secondsOnPage >= 90)
                {
                    response.Message = "😊 চেকআউটে সমস্যা হচ্ছে? আমি সাহায্য করতে পারি!\n\nপেমেন্ট, এড্রেস বা অন্য কোনো বিষয়ে প্রশ্ন থাকলে বলুন।";
                    response.QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "💬 এজেন্টের সাথে কথা", Action = "send_message", Payload = "human agent", Style = "primary" },
                        new() { Text = "📞 কল করুন", Action = "send_message", Payload = "helpline number", Style = "warning" }
                    };
                    response.TypingDelayMs = 2000;
                    return response;
                }

                // General browsing
                if (secondsOnPage >= 120)
                {
                    response.Message = "👋 কিছু খুঁজছেন? আমি সাহায্য করতে পারি!\n\nপণ্য খোঁজা, দাম জানা, অর্ডার ট্র্যাক - যেকোনো কিছুতে সাহায্য করতে পারি।";
                    response.QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🛍️ পণ্য খুঁজুন", Action = "send_message", Payload = "পণ্য দেখাও", Style = "primary" },
                        new() { Text = "🔥 ট্রেন্ডিং", Action = "send_message", Payload = "trending products", Style = "warning" },
                        new() { Text = "🎁 অফার", Action = "send_message", Payload = "today offer", Style = "success" }
                    };
                    response.TypingDelayMs = 1000;
                    return response;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating proactive message");
                return null;
            }
        }

        #endregion

        #region Conversation Analytics

        // Track analytics per session
        private static readonly Dictionary<int, SessionAnalyticsData> _sessionAnalytics = new();

        /// <summary>
        /// Get conversation analytics for a session
        /// </summary>
        public async Task<ConversationAnalytics> GetSessionAnalyticsAsync(int sessionId)
        {
            var analytics = new ConversationAnalytics { SessionId = sessionId };

            try
            {
                if (_conversationContexts.TryGetValue(sessionId, out var context))
                {
                    analytics.TotalMessages = context.MessageCount;
                    analytics.DetectedIntents = context.ConversationTopics.Distinct().ToList();
                    analytics.MentionedProducts = context.MentionedProducts.Distinct().ToList();
                    analytics.PrimarySentiment = context.DetectedSentiment;
                    analytics.Duration = DateTime.UtcNow - context.CreatedAt;
                }

                if (_sessionAnalytics.TryGetValue(sessionId, out var data))
                {
                    analytics.AIResponses = data.AIResponseCount;
                    analytics.AverageConfidence = data.ConfidenceScores.Any() ? data.ConfidenceScores.Average() : 1.0;
                    analytics.UnresolvedQueries = data.UnresolvedCount;
                }

                if (_handoffTrackers.TryGetValue(sessionId, out var tracker))
                {
                    analytics.WasHandedOff = tracker.UserRequestedHuman;
                }

                // Get feedback stats
                var sessionFeedback = _feedbackStore.Where(f => f.SessionId == sessionId).ToList();
                analytics.PositiveFeedback = sessionFeedback.Count(f => f.IsHelpful);
                analytics.NegativeFeedback = sessionFeedback.Count(f => !f.IsHelpful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session analytics for {SessionId}", sessionId);
            }

            return await Task.FromResult(analytics);
        }

        /// <summary>
        /// Track analytics data for a response
        /// </summary>
        private void TrackResponseAnalytics(int sessionId, double confidence, string intent, bool wasResolved)
        {
            if (!_sessionAnalytics.TryGetValue(sessionId, out var data))
            {
                data = new SessionAnalyticsData { SessionId = sessionId };
                _sessionAnalytics[sessionId] = data;
            }

            data.AIResponseCount++;
            data.ConfidenceScores.Add(confidence);
            data.Intents.Add(intent);

            if (!wasResolved)
            {
                data.UnresolvedCount++;
            }
        }

        private class SessionAnalyticsData
        {
            public int SessionId { get; set; }
            public int AIResponseCount { get; set; }
            public List<double> ConfidenceScores { get; set; } = new();
            public List<string> Intents { get; set; } = new();
            public int UnresolvedCount { get; set; }
        }

        #endregion

        #region Enhanced Response Builder

        /// <summary>
        /// Enhance response with quick replies, cross-sell, and urgency
        /// </summary>
        private async Task<AIChatResponse> EnhanceResponseAsync(AIChatResponse response, string intent, ConversationContext context, string? userId)
        {
            // Add typing delay based on message length
            response.TypingDelayMs = Math.Min(response.Message.Length * 20, 3000);

            // Add quick replies
            response.QuickReplies = GenerateQuickReplies(intent, context);

            // Add metadata
            response.DetectedIntent = intent;
            response.DetectedSentiment = context.DetectedSentiment;

            // Add cross-sell if product related
            if (intent == "product_search" && response.ProductSuggestions?.Any() == true)
            {
                var firstProductId = response.ProductSuggestions.First().Id;
                response.CrossSellProducts = await GetCrossSellProductsAsync(firstProductId, null, context);
                response.Urgency = await GetUrgencyMessageAsync(firstProductId);
            }

            // Check for handoff
            var handoffDecision = await ShouldHandoffToHumanAsync(context.SessionId);
            if (handoffDecision.ShouldHandoff)
            {
                response.RequiresHumanHandoff = true;
                response.HandoffReason = handoffDecision.Reason;

                // Add handoff message
                response.Message += "\n\n💬 আপনি চাইলে আমাদের একজন এজেন্টের সাথে সরাসরি কথা বলতে পারেন।";
                response.QuickReplies?.Add(new QuickReplyButton
                {
                    Text = "👨‍💼 এজেন্টের সাথে কথা বলুন",
                    Action = "send_message",
                    Payload = "human agent connect করুন",
                    Style = "primary"
                });
            }

            // Track analytics
            TrackResponseAnalytics(context.SessionId, response.ConfidenceScore, intent, intent != "unknown");

            return response;
        }

        #endregion

        #region Cart Integration

        private class PendingCartAction
        {
            public int SessionId { get; set; }
            public int? ProductId { get; set; }
            public string? ProductName { get; set; }
            public int Quantity { get; set; } = 1;
            public int? VariantId { get; set; }
            public string? Size { get; set; }
            public string? Color { get; set; }
            public bool AwaitingConfirmation { get; set; }
            public bool AwaitingVariantSelection { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Handle add to cart intent from conversation
        /// </summary>
        private async Task<AIChatResponse> HandleAddToCartIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context, string? userId)
        {
            try
            {
                // Check if there's a pending cart action awaiting confirmation
                if (_pendingCartActions.TryGetValue(context.SessionId, out var pendingAction) && pendingAction.AwaitingConfirmation)
                {
                    // Expanded confirmation keywords - includes Banglish, Bengali, and English variations
                    var confirmationKeywords = new[] {
                        // English
                        "yes", "yeah", "yep", "yup", "ok", "okay", "sure", "confirm", "add", "right", "correct", "fine",
                        // Banglish
                        "han", "haa", "ha ", " ha", "hmm", "ji", "jee", "accha", "thik", "lagao", "dao", "deo", "diyo",
                        "koro", "korbo", "kori", "din", "den", "deya", "nao", "nibo", "nebo", "jog", "rakh", "loi",
                        // Bengali
                        "হ্যাঁ", "হা", "হাঁ", "আচ্ছা", "ঠিক", "করো", "করি", "করুন", "দাও", "দিন", "দেন", "দিয়ে",
                        "যোগ", "লাগাও", "নিব", "নেব", "নাও", "চাই", "রাখ", "লই", "দে"
                    };

                    // Rejection keywords
                    var rejectionKeywords = new[] {
                        "no", "nah", "nope", "cancel", "stop", "thak", "na ", " na", "naa", "lagbena", "dorkar nai",
                        "না", "থাক", "লাগবেনা", "দরকার নাই", "বাদ", "রাখ"
                    };

                    // Check for confirmation
                    bool isConfirmation = confirmationKeywords.Any(k => normalizedMessage.Contains(k) || originalMessage.ToLower().Contains(k));
                    bool isRejection = rejectionKeywords.Any(k => normalizedMessage.Contains(k) || originalMessage.ToLower().Contains(k));

                    if (isConfirmation && !isRejection)
                    {
                        var result = await AddToCartViaChatAsync(pendingAction.ProductId!.Value, pendingAction.Quantity, userId, pendingAction.VariantId);

                        // Update context with the product that was just added to cart
                        context.LastMentionedProductId = pendingAction.ProductId;
                        context.LastProductQuery = pendingAction.ProductName;

                        _pendingCartActions.Remove(context.SessionId);
                        return new AIChatResponse
                        {
                            Message = result.Message,
                            QuickReplies = result.QuickReplies
                        };
                    }
                    else if (isRejection)
                    {
                        _pendingCartActions.Remove(context.SessionId);
                        return new AIChatResponse
                        {
                            Message = "ঠিক আছে! কার্টে যোগ করলাম না। 😊\n\nঅন্য কিছু দেখতে চাইলে বলুন!",
                            QuickReplies = new List<QuickReplyButton>
                            {
                                new() { Text = "🛍️ পণ্য দেখুন", Action = "send_message", Payload = "পণ্য দেখাও", Style = "primary" },
                                new() { Text = "🔍 অন্য কিছু খুঁজুন", Action = "send_message", Payload = "অন্য পণ্য", Style = "default" }
                            }
                        };
                    }
                    // If neither confirmation nor rejection, keep the pending action and remind user
                    else
                    {
                        return new AIChatResponse
                        {
                            Message = $"🛒 **\"{pendingAction.ProductName}\"** কার্টে যোগ করার অপেক্ষায় আছি!\n\n" +
                                      "**হ্যাঁ** বলুন যোগ করতে, অথবা **না** বলুন বাদ দিতে।",
                            QuickReplies = new List<QuickReplyButton>
                            {
                                new() { Text = "✅ হ্যাঁ, যোগ করুন", Action = "send_message", Payload = "হ্যাঁ কার্টে যোগ করুন", Style = "success" },
                                new() { Text = "❌ না থাক", Action = "send_message", Payload = "না থাক", Style = "default" }
                            }
                        };
                    }
                }

                // Extract product info from message or context
                var productName = ExtractProductQuery(normalizedMessage, originalMessage);

                // If no product in message, check last viewed product in context
                if (string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(context.LastProductQuery))
                {
                    productName = context.LastProductQuery;
                }

                // If we have a recently mentioned product
                if (!string.IsNullOrEmpty(productName))
                {
                    // Search for the product
                    var searchResult = await SearchProductsWithLinksAsync(productName, 3);

                    if (searchResult.Found && searchResult.Products.Any())
                    {
                        var product = searchResult.Products.First();

                        // Check if product has variants
                        if (product.HasVariants && product.Variants?.Any() == true)
                        {
                            // Ask for variant selection
                            _pendingCartActions[context.SessionId] = new PendingCartAction
                            {
                                SessionId = context.SessionId,
                                ProductId = product.Id,
                                ProductName = product.Name,
                                AwaitingVariantSelection = true
                            };

                            var variantOptions = product.Variants
                                .Where(v => v.Stock > 0)
                                .Select(v => $"• {v.Size ?? ""} {v.Color ?? ""}".Trim())
                                .Take(6);

                            return new AIChatResponse
                            {
                                Message = $"🛒 **{product.Name}** কার্টে যোগ করতে চান!\n\n" +
                                          $"💰 দাম: {(product.DiscountPrice.HasValue ? $"~~৳{product.Price:N0}~~ **৳{product.DiscountPrice:N0}**" : $"**৳{product.Price:N0}**")}\n\n" +
                                          $"📏 **সাইজ/কালার বলুন:**\n{string.Join("\n", variantOptions)}\n\n" +
                                          "কোনটা চান? সাইজ/কালার লিখুন।",
                                QuickReplies = product.Variants.Where(v => v.Stock > 0).Take(4).Select(v => new QuickReplyButton
                                {
                                    Text = $"{v.Size ?? ""} {v.Color ?? ""}".Trim(),
                                    Action = "send_message",
                                    Payload = $"{product.Name} {v.Size ?? ""} {v.Color ?? ""}".Trim(),
                                    Style = "primary"
                                }).ToList()
                            };
                        }

                        // No variants - confirm add to cart
                        _pendingCartActions[context.SessionId] = new PendingCartAction
                        {
                            SessionId = context.SessionId,
                            ProductId = product.Id,
                            ProductName = product.Name,
                            Quantity = 1,
                            AwaitingConfirmation = true
                        };

                        var priceText = product.DiscountPrice.HasValue
                            ? $"~~৳{product.Price:N0}~~ **৳{product.DiscountPrice:N0}**"
                            : $"**৳{product.Price:N0}**";

                        return new AIChatResponse
                        {
                            Message = $"🛒 **\"{product.Name}\"** কার্টে যোগ করব?\n\n" +
                                      $"💰 দাম: {priceText}\n" +
                                      $"📦 স্টক: {(product.InStock ? $"✅ আছে ({product.Stock}টি)" : "❌ স্টক আউট")}\n" +
                                      $"🔗 [পণ্য দেখুন]({product.ProductUrl})\n\n" +
                                      "**কার্টে যোগ করব?**",
                            QuickReplies = new List<QuickReplyButton>
                            {
                                new() { Text = "✅ হ্যাঁ, যোগ করুন", Action = "send_message", Payload = "হ্যাঁ কার্টে যোগ করুন", Style = "success" },
                                new() { Text = "🔢 ২টা নিব", Action = "send_message", Payload = $"{product.Name} 2 ta", Style = "primary" },
                                new() { Text = "❌ না থাক", Action = "send_message", Payload = "না থাক", Style = "default" },
                                new() { Text = "🔍 আরো দেখুন", Action = "send_message", Payload = productName, Style = "default" }
                            },
                            ProductSuggestions = new List<AIProductSuggestion>
                            {
                                new() { Id = product.Id, Name = product.Name, Price = product.Price, DiscountPrice = product.DiscountPrice, ImageUrl = product.ImageUrl, Slug = product.Slug }
                            }
                        };
                    }
                }

                // No product found - ask user
                return new AIChatResponse
                {
                    Message = "🛒 কার্টে কোন পণ্য যোগ করতে চান?\n\n" +
                              "পণ্যের নাম লিখুন, আমি খুঁজে দেখি!\n\n" +
                              "**উদাহরণ:**\n" +
                              "• \"শাড়ি কার্টে দাও\"\n" +
                              "• \"এটা নিব\" (আগের পণ্যটি)\n" +
                              "• \"তিন সেট tshirt কিনব\"",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🛍️ পণ্য খুঁজুন", Action = "send_message", Payload = "পণ্য দেখাও", Style = "primary" },
                        new() { Text = "🔥 ট্রেন্ডিং", Action = "send_message", Payload = "trending products", Style = "warning" },
                        new() { Text = "🛒 কার্ট দেখুন", Action = "send_message", Payload = "আমার কার্ট দেখাও", Style = "default" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling add to cart intent");
                return new AIChatResponse
                {
                    Message = "দুঃখিত! কার্টে যোগ করতে সমস্যা হচ্ছে। 😔\n\nআবার চেষ্টা করুন অথবা সরাসরি পণ্য পেজ থেকে যোগ করুন।",
                    IsSuccessful = false
                };
            }
        }

        /// <summary>
        /// Handle view cart request
        /// </summary>
        private async Task<AIChatResponse> HandleViewCartAsync(string? userId)
        {
            try
            {
                var cartItems = await _cartService.GetCartItemsAsync(userId);

                if (!cartItems.Any())
                {
                    return new AIChatResponse
                    {
                        Message = "🛒 আপনার কার্ট খালি!\n\n" +
                                  "কিছু শপিং করুন? আমি সাহায্য করতে পারি! 😊",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🛍️ পণ্য দেখুন", Action = "send_message", Payload = "পণ্য দেখাও", Style = "primary" },
                            new() { Text = "🔥 বেস্ট সেলার", Action = "send_message", Payload = "best seller", Style = "warning" },
                            new() { Text = "🎁 অফার", Action = "send_message", Payload = "discount offer", Style = "success" }
                        }
                    };
                }

                var cartTotal = cartItems.Sum(c => c.EffectivePrice * c.Quantity);
                var itemList = cartItems.Select(c =>
                    $"• **{c.Product?.Name ?? "পণ্য"}** x{c.Quantity} = ৳{(c.EffectivePrice * c.Quantity):N0}"
                );

                return new AIChatResponse
                {
                    Message = $"🛒 **আপনার কার্ট ({cartItems.Count}টি পণ্য)**\n\n" +
                              string.Join("\n", itemList) +
                              $"\n\n💰 **মোট: ৳{cartTotal:N0}**\n\n" +
                              "🔗 [কার্ট দেখুন](/Customer/Home/Cart) | [চেকআউট করুন](/Customer/Home/Checkout)",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "✅ চেকআউট", Icon = "check-circle", Action = "open_url", Payload = "/Customer/Home/Checkout", Style = "success" },
                        new() { Text = "🛒 কার্ট দেখুন", Icon = "shopping-cart", Action = "open_url", Payload = "/Customer/Home/Cart", Style = "primary" },
                        new() { Text = "🛍️ আরো শপিং", Action = "send_message", Payload = "আরো পণ্য দেখাও", Style = "default" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing cart");
                return new AIChatResponse
                {
                    Message = "কার্ট দেখতে সমস্যা হচ্ছে। 😔 সরাসরি কার্ট পেজে যান: /Customer/Home/Cart",
                    IsSuccessful = false
                };
            }
        }

        /// <summary>
        /// Add product to cart via chat
        /// </summary>
        public async Task<AIAddToCartResponse> AddToCartViaChatAsync(int productId, int quantity, string? userId, int? variantId = null)
        {
            try
            {
                var product = await _db.Products
                    .Include(p => p.Seller)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return new AIAddToCartResponse
                    {
                        Success = false,
                        Message = "দুঃখিত! পণ্যটি পাওয়া যায়নি। 😔"
                    };
                }

                // Check if user is authenticated - guest cart via SignalR doesn't work properly
                if (string.IsNullOrEmpty(userId))
                {
                    var productUrl = $"/Customer/Home/Details/{product.Id}";
                    return new AIAddToCartResponse
                    {
                        Success = false,
                        Message = $"🛒 **\"{product.Name}\"** কার্টে যোগ করতে:\n\n" +
                                  $"👉 [পণ্য পেজে যান]({productUrl}) এবং সেখান থেকে কার্টে যোগ করুন।\n\n" +
                                  "অথবা **লগইন করুন** চ্যাট থেকে সরাসরি কার্টে যোগ করতে! 😊",
                        ProductName = product.Name,
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🛍️ পণ্য পেজে যান", Icon = "external-link", Action = "open_url", Payload = productUrl, Style = "primary" },
                            new() { Text = "🔐 লগইন করুন", Icon = "log-in", Action = "open_url", Payload = "/Identity/Account/Login", Style = "success" }
                        }
                    };
                }

                if (product.Stock < quantity)
                {
                    return new AIAddToCartResponse
                    {
                        Success = false,
                        Message = $"দুঃখিত! \"{product.Name}\" এ পর্যাপ্ত স্টক নেই। 😔\n\nবর্তমান স্টক: {product.Stock}টি",
                        ProductName = product.Name
                    };
                }

                var result = await _cartService.AddToCartAsync(productId, quantity, userId, variantId);

                if (result.ShopConflict)
                {
                    return new AIAddToCartResponse
                    {
                        Success = false,
                        ShopConflict = true,
                        Message = $"⚠️ আপনার কার্টে \"{result.ExistingShopName}\" এর পণ্য আছে।\n\n" +
                                  "একই কার্টে একাধিক দোকানের পণ্য রাখা যায় না।\n\n" +
                                  "**কি করতে চান?**",
                        ConflictMessage = result.Message,
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🔄 কার্ট ক্লিয়ার করে যোগ করুন", Action = "send_message", Payload = $"clear cart and add {product.Name}", Style = "warning" },
                            new() { Text = "🛒 আগের কার্ট রাখুন", Action = "send_message", Payload = "কার্ট দেখাও", Style = "default" }
                        }
                    };
                }

                if (result.RequiresVariant)
                {
                    var productUrl = $"/Customer/Home/Details/{product.Id}";
                    return new AIAddToCartResponse
                    {
                        Success = false,
                        RequiresVariant = true,
                        Message = $"📏 **\"{product.Name}\"** এ সাইজ/কালার সিলেক্ট করতে হবে!\n\n" +
                                  $"👉 [পণ্য পেজে যান]({productUrl}) এবং সাইজ/কালার সিলেক্ট করে কার্টে যোগ করুন।",
                        ProductName = product.Name,
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🛍️ পণ্য পেজে যান", Icon = "external-link", Action = "open_url", Payload = productUrl, Style = "primary" }
                        }
                    };
                }

                if (!result.Success)
                {
                    return new AIAddToCartResponse
                    {
                        Success = false,
                        Message = result.Message ?? "কার্টে যোগ করতে সমস্যা হয়েছে।"
                    };
                }

                var price = product.DiscountPrice ?? product.Price;
                var totalPrice = price * quantity;

                return new AIAddToCartResponse
                {
                    Success = true,
                    Message = $"✅ **কার্টে যোগ হয়েছে!**\n\n" +
                              $"🛍️ **{product.Name}**\n" +
                              $"📦 পরিমাণ: {quantity}টি\n" +
                              $"💰 মূল্য: ৳{totalPrice:N0}\n\n" +
                              "🔗 [কার্ট দেখুন](/Customer/Home/Cart) | [চেকআউট করুন](/Customer/Home/Checkout)",
                    ProductName = product.Name,
                    Quantity = quantity,
                    TotalPrice = totalPrice,
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "✅ চেকআউট করুন", Icon = "check", Action = "open_url", Payload = "/Customer/Home/Checkout", Style = "success" },
                        new() { Text = "🛒 কার্ট দেখুন", Icon = "cart", Action = "open_url", Payload = "/Customer/Home/Cart", Style = "primary" },
                        new() { Text = "🛍️ আরো শপিং", Action = "send_message", Payload = "আরো পণ্য দেখাও", Style = "default" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart via chat");
                return new AIAddToCartResponse
                {
                    Success = false,
                    Message = "কার্টে যোগ করতে সমস্যা হয়েছে। দুঃখিত! 😔"
                };
            }
        }

        /// <summary>
        /// Search products with direct links
        /// </summary>
        public async Task<AIProductSearchResult> SearchProductsWithLinksAsync(string query, int maxResults = 5)
        {
            try
            {
                var normalizedQuery = NormalizeBanglish(query.ToLower());

                var products = await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .Where(p => p.Status == ProductStatus.Active &&
                               (p.Name.Contains(query) ||
                                p.Name.ToLower().Contains(normalizedQuery) ||
                                (p.Tags != null && p.Tags.Contains(query)) ||
                                (p.Category != null && p.Category.Name.Contains(query))))
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.SoldCount)
                    .Take(maxResults)
                    .ToListAsync();

                if (!products.Any())
                {
                    // Try fuzzy search
                    var allProducts = await _db.Products
                        .Include(p => p.Category)
                        .Include(p => p.Seller)
                        .Include(p => p.Variants)
                        .Include(p => p.Reviews)
                        .Where(p => p.Status == ProductStatus.Active)
                        .Take(100)
                        .ToListAsync();

                    products = allProducts
                        .Where(p => CalculateSimilarity(NormalizeBanglish(p.Name.ToLower()), normalizedQuery) > 0.4)
                        .OrderByDescending(p => CalculateSimilarity(NormalizeBanglish(p.Name.ToLower()), normalizedQuery))
                        .Take(maxResults)
                        .ToList();
                }

                if (!products.Any())
                {
                    return new AIProductSearchResult
                    {
                        Found = false,
                        Message = $"দুঃখিত! \"{query}\" খুঁজে পাওয়া যায়নি। 😔\n\nঅন্য কিছু খুঁজুন?",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🔥 ট্রেন্ডিং", Action = "send_message", Payload = "trending products", Style = "warning" },
                            new() { Text = "🆕 নতুন পণ্য", Action = "send_message", Payload = "new arrival", Style = "primary" }
                        }
                    };
                }

                var productLinks = products.Select(p => new AIProductWithLink
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    ImageUrl = p.ImageUrl,
                    Slug = p.Slug ?? p.Id.ToString(),
                    ProductUrl = $"/Customer/Home/Details/{p.Slug ?? p.Id.ToString()}",
                    CategoryName = p.Category?.Name,
                    Stock = p.Stock,
                    SellerName = p.Seller?.ShopName,
                    Rating = p.Seller?.Rating,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    HasVariants = p.Variants?.Any(v => v.IsAvailable) == true,
                    Variants = p.Variants?.Where(v => v.IsAvailable).Select(v => new AIProductVariant
                    {
                        Id = v.Id,
                        Size = v.Size,
                        Color = v.Color,
                        Stock = v.Stock,
                        PriceAdjustment = v.AdditionalPrice
                    }).ToList()
                }).ToList();

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"🔍 **\"{query}\" এর জন্য {products.Count}টি পণ্য পাওয়া গেছে:**\n");

                foreach (var product in productLinks)
                {
                    var priceText = product.DiscountPrice.HasValue
                        ? $"~~৳{product.Price:N0}~~ **৳{product.DiscountPrice:N0}**"
                        : $"**৳{product.Price:N0}**";

                    var stockText = product.InStock ? "✅" : "❌";

                    messageBuilder.AppendLine($"🛍️ **{product.Name}**");
                    messageBuilder.AppendLine($"   {priceText} {stockText}");
                    messageBuilder.AppendLine($"   🔗 [দেখুন]({product.ProductUrl}) | [কিনুন]({product.ProductUrl})\n");
                }

                messageBuilder.AppendLine("\n💡 **কিনতে চাইলে পণ্যের নাম লিখে \"কিনব\" বলুন!**");

                return new AIProductSearchResult
                {
                    Found = true,
                    Message = messageBuilder.ToString(),
                    Products = productLinks,
                    QuickReplies = productLinks.Take(3).Select(p => new QuickReplyButton
                    {
                        Text = $"🛒 {p.Name.Substring(0, Math.Min(p.Name.Length, 15))}...",
                        Action = "send_message",
                        Payload = $"{p.Name} কিনব",
                        Style = "primary"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with links");
                return new AIProductSearchResult
                {
                    Found = false,
                    Message = "পণ্য খুঁজতে সমস্যা হয়েছে। আবার চেষ্টা করুন।"
                };
            }
        }

        /// <summary>
        /// Get personalized recommendations based on conversation
        /// </summary>
        public async Task<AIChatResponse> GetConversationBasedRecommendationsAsync(int sessionId, string? userId)
        {
            try
            {
                if (!_conversationContexts.TryGetValue(sessionId, out var context))
                {
                    return new AIChatResponse
                    {
                        Message = "🛍️ আপনার জন্য রেকমেন্ডেশন পেতে কিছু প্রশ্ন করুন বা পণ্য খুঁজুন! 😊"
                    };
                }

                var recommendations = new List<AIProductWithLink>();

                // Based on mentioned products
                if (context.MentionedProducts.Any())
                {
                    foreach (var productName in context.MentionedProducts.Distinct().Take(3))
                    {
                        var searchResult = await SearchProductsWithLinksAsync(productName, 2);
                        if (searchResult.Found)
                        {
                            recommendations.AddRange(searchResult.Products);
                        }
                    }
                }

                // Based on last category
                if (!string.IsNullOrEmpty(context.LastCategory))
                {
                    var categoryProducts = await _db.Products
                        .Include(p => p.Category)
                        .Where(p => p.Status == ProductStatus.Active &&
                                   p.Category != null &&
                                   p.Category.Name.Contains(context.LastCategory))
                        .OrderByDescending(p => p.SoldCount)
                        .Take(3)
                        .Select(p => new AIProductWithLink
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Price = p.Price,
                            DiscountPrice = p.DiscountPrice,
                            ImageUrl = p.ImageUrl,
                            Slug = p.Slug ?? p.Id.ToString(),
                            ProductUrl = $"/Customer/Home/Details/{p.Slug ?? p.Id.ToString()}",
                            CategoryName = p.Category!.Name,
                            Stock = p.Stock
                        })
                        .ToListAsync();

                    recommendations.AddRange(categoryProducts);
                }

                // Based on user preferences
                if (!string.IsNullOrEmpty(userId))
                {
                    var prefs = GetOrCreateUserPreferences(userId);
                    foreach (var category in prefs.InterestedCategories.Take(2))
                    {
                        var catProducts = await _db.Products
                            .Include(p => p.Category)
                            .Where(p => p.Status == ProductStatus.Active &&
                                       p.Category != null &&
                                       p.Category.Name.Contains(category))
                            .OrderByDescending(p => p.IsFeatured)
                            .Take(2)
                            .Select(p => new AIProductWithLink
                            {
                                Id = p.Id,
                                Name = p.Name,
                                Price = p.Price,
                                DiscountPrice = p.DiscountPrice,
                                ProductUrl = $"/Customer/Home/Details/{p.Slug ?? p.Id.ToString()}",
                                Stock = p.Stock
                            })
                            .ToListAsync();

                        recommendations.AddRange(catProducts);
                    }
                }

                if (!recommendations.Any())
                {
                    // Get trending products
                    recommendations = await _db.Products
                        .Where(p => p.Status == ProductStatus.Active)
                        .OrderByDescending(p => p.SoldCount)
                        .Take(5)
                        .Select(p => new AIProductWithLink
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Price = p.Price,
                            DiscountPrice = p.DiscountPrice,
                            ProductUrl = $"/Customer/Home/Details/{p.Slug ?? p.Id.ToString()}",
                            Stock = p.Stock
                        })
                        .ToListAsync();
                }

                var uniqueRecs = recommendations.DistinctBy(p => p.Id).Take(5).ToList();

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("🎯 **আপনার কথোপকথন অনুযায়ী রেকমেন্ডেশন:**\n");

                foreach (var product in uniqueRecs)
                {
                    var priceText = product.DiscountPrice.HasValue
                        ? $"~~৳{product.Price:N0}~~ **৳{product.DiscountPrice:N0}**"
                        : $"**৳{product.Price:N0}**";

                    messageBuilder.AppendLine($"⭐ **{product.Name}**");
                    messageBuilder.AppendLine($"   {priceText}");
                    messageBuilder.AppendLine($"   🔗 [দেখুন]({product.ProductUrl})\n");
                }

                messageBuilder.AppendLine("\n💡 কিনতে চাইলে পণ্যের নাম বলুন!");

                return new AIChatResponse
                {
                    Message = messageBuilder.ToString(),
                    ProductSuggestions = uniqueRecs.Select(p => new AIProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        Slug = p.Slug
                    }).ToList(),
                    QuickReplies = uniqueRecs.Take(3).Select(p => new QuickReplyButton
                    {
                        Text = $"🛒 {p.Name.Substring(0, Math.Min(p.Name.Length, 12))}...",
                        Action = "send_message",
                        Payload = $"{p.Name} কিনব",
                        Style = "primary"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation-based recommendations");
                return new AIChatResponse
                {
                    Message = "রেকমেন্ডেশন পেতে সমস্যা হয়েছে। আবার চেষ্টা করুন।",
                    IsSuccessful = false
                };
            }
        }

        #endregion

        #region Advanced Features - Order Tracking

        /// <summary>
        /// Handle track order intent
        /// </summary>
        private async Task<AIChatResponse> HandleTrackOrderIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context, string? userId)
        {
            var orderNumber = ExtractOrderNumber(originalMessage) ?? ExtractOrderNumber(normalizedMessage);

            // If no order number in message, try from context
            if (string.IsNullOrEmpty(orderNumber) && !string.IsNullOrEmpty(context.LastOrderNumber))
            {
                orderNumber = context.LastOrderNumber;
            }

            if (!string.IsNullOrEmpty(orderNumber))
            {
                // Save to context for future reference
                context.LastOrderNumber = orderNumber;

                var result = await TrackOrderAsync(orderNumber, userId);
                return new AIChatResponse
                {
                    Message = result.Message,
                    QuickReplies = result.QuickReplies,
                    DetectedIntent = "track_order"
                };
            }

            // No order number provided - ask for it or show recent orders
            if (!string.IsNullOrEmpty(userId))
            {
                var recentOrders = await GetRecentOrdersAsync(userId, 3);
                if (recentOrders.Found && recentOrders.Orders?.Any() == true)
                {
                    return new AIChatResponse
                    {
                        Message = "📦 **অর্ডার ট্র্যাক করতে চান?**\n\n" +
                                  "অর্ডার নম্বর দিন, অথবা আপনার সাম্প্রতিক অর্ডার থেকে বেছে নিন:\n\n" +
                                  string.Join("\n", recentOrders.Orders.Take(3).Select(o =>
                                      $"• **{o.OrderNumber}** - {o.StatusBangla} - ৳{o.TotalAmount:N0}")),
                        QuickReplies = recentOrders.Orders.Take(3).Select(o => new QuickReplyButton
                        {
                            Text = $"📦 {o.OrderNumber}",
                            Action = "send_message",
                            Payload = $"order {o.OrderNumber} track koro",
                            Style = o.Status == "Shipped" ? "warning" : "default"
                        }).ToList(),
                        DetectedIntent = "track_order"
                    };
                }
            }

            return new AIChatResponse
            {
                Message = "📦 **অর্ডার ট্র্যাক করতে চান?**\n\n" +
                          "আপনার অর্ডার নম্বর দিন (যেমন: BLY-20240115-1234)।\n\n" +
                          "💡 অর্ডার নম্বর আপনার ইমেইল বা SMS এ পাঠানো হয়েছে।",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "📋 আমার সব অর্ডার", Action = "send_message", Payload = "amar sob order dekhao", Style = "primary" },
                    new() { Text = "📞 হেল্পলাইন", Action = "send_message", Payload = "helpline", Style = "default" }
                },
                DetectedIntent = "track_order"
            };
        }

        /// <summary>
        /// Handle my orders intent
        /// </summary>
        private async Task<AIChatResponse> HandleMyOrdersIntentAsync(ConversationContext context, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse
                {
                    Message = "😊 আপনার অর্ডার দেখতে লগইন করতে হবে!\n\n" +
                              "🔗 [লগইন করুন](/Identity/Account/Login)\n\n" +
                              "অথবা অর্ডার নম্বর দিয়ে track করতে পারেন।",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🔐 লগইন", Action = "open_url", Payload = "/Identity/Account/Login", Style = "primary" },
                        new() { Text = "📦 অর্ডার ট্র্যাক", Action = "send_message", Payload = "order track korte chai", Style = "default" }
                    }
                };
            }

            var result = await GetRecentOrdersAsync(userId, 5);

            // Save first order number to context for future reference
            if (result.Found && result.Orders?.Any() == true)
            {
                context.LastOrderNumber = result.Orders.First().OrderNumber;
            }

            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies,
                DetectedIntent = "my_orders"
            };
        }

        /// <summary>
        /// Track order by order number
        /// </summary>
        public async Task<AIOrderTrackingResponse> TrackOrderAsync(string? orderNumber, string? userId)
        {
            try
            {
                if (string.IsNullOrEmpty(orderNumber))
                {
                    return new AIOrderTrackingResponse
                    {
                        Found = false,
                        Message = "অর্ডার নম্বর দিন (যেমন: BLY-20240115-1234)"
                    };
                }

                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.Division)
                    .Include(o => o.District)
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber ||
                                              o.OrderNumber!.Contains(orderNumber) ||
                                              o.Id.ToString() == orderNumber);

                if (order == null)
                {
                    return new AIOrderTrackingResponse
                    {
                        Found = false,
                        Message = $"😕 দুঃখিত! **{orderNumber}** নম্বরের কোনো অর্ডার খুঁজে পাওয়া যায়নি।\n\n" +
                                  "অনুগ্রহ করে সঠিক অর্ডার নম্বর দিন অথবা আমাদের হেল্পলাইনে যোগাযোগ করুন।",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🔄 আবার চেষ্টা", Action = "send_message", Payload = "order track", Style = "default" },
                            new() { Text = "📞 হেল্পলাইন", Action = "send_message", Payload = "helpline", Style = "primary" }
                        }
                    };
                }

                // Check if user is authorized to see this order
                if (!string.IsNullOrEmpty(userId) && order.UserId != userId)
                {
                    // Still show if guest order with matching email/phone
                    // For now, allow viewing
                }

                var orderInfo = MapToOrderInfo(order);
                var statusMessage = GetOrderStatusMessage(order);

                return new AIOrderTrackingResponse
                {
                    Found = true,
                    Order = orderInfo,
                    Message = $"📦 **অর্ডার #{order.OrderNumber}**\n\n" +
                              $"📅 তারিখ: {order.OrderDate:dd MMM yyyy}\n" +
                              $"📍 স্ট্যাটাস: **{orderInfo.StatusBangla}**\n" +
                              $"💰 মোট: ৳{order.TotalAmount:N0}\n\n" +
                              statusMessage +
                              (order.TrackingNumber != null ? $"\n\n🚚 ট্র্যাকিং: {order.TrackingNumber} ({order.CourierName ?? "Courier"})" : "") +
                              $"\n\n🔗 [অর্ডার বিস্তারিত দেখুন](/Customer/Home/OrderDetails/{order.Id})",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "📋 বিস্তারিত", Action = "open_url", Payload = $"/Customer/Home/OrderDetails/{order.Id}", Style = "primary" },
                        orderInfo.CanCancel ? new() { Text = "❌ বাতিল করুন", Action = "send_message", Payload = $"order {order.OrderNumber} cancel korte chai", Style = "danger" } : null!,
                        orderInfo.CanReturn ? new() { Text = "↩️ রিটার্ন", Action = "send_message", Payload = $"order {order.OrderNumber} return korte chai", Style = "warning" } : null!,
                        new() { Text = "📦 অন্য অর্ডার", Action = "send_message", Payload = "amar sob order dekhao", Style = "default" }
                    }.Where(q => q != null).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order {OrderNumber}", orderNumber);
                return new AIOrderTrackingResponse
                {
                    Found = false,
                    Message = "অর্ডার ট্র্যাক করতে সমস্যা হয়েছে। আবার চেষ্টা করুন।"
                };
            }
        }

        /// <summary>
        /// Get user's recent orders
        /// </summary>
        public async Task<AIOrderTrackingResponse> GetRecentOrdersAsync(string userId, int count = 5)
        {
            try
            {
                var orders = await _db.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(count)
                    .ToListAsync();

                if (!orders.Any())
                {
                    return new AIOrderTrackingResponse
                    {
                        Found = false,
                        Message = "😊 আপনার এখনো কোনো অর্ডার নেই!\n\n" +
                                  "আজই শপিং শুরু করুন এবং আমাদের দারুণ সব পণ্য দেখুন! 🛍️",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🛍️ শপিং শুরু", Action = "open_url", Payload = "/Customer/Home", Style = "primary" },
                            new() { Text = "🔥 বেস্ট সেলার", Action = "send_message", Payload = "best seller dekhao", Style = "success" }
                        }
                    };
                }

                var orderInfos = orders.Select(MapToOrderInfo).ToList();
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"📦 **আপনার সাম্প্রতিক অর্ডার ({orders.Count}টি):**\n");

                foreach (var order in orderInfos)
                {
                    var statusIcon = order.Status switch
                    {
                        "Delivered" => "✅",
                        "Shipped" or "OutForDelivery" => "🚚",
                        "Processing" => "⏳",
                        "Cancelled" => "❌",
                        "Returned" => "↩️",
                        _ => "📦"
                    };

                    messageBuilder.AppendLine($"{statusIcon} **{order.OrderNumber}** - {order.StatusBangla}");
                    messageBuilder.AppendLine($"   📅 {order.OrderDate:dd MMM yyyy} | 💰 ৳{order.TotalAmount:N0}");
                    messageBuilder.AppendLine();
                }

                messageBuilder.AppendLine("🔗 [সব অর্ডার দেখুন](/Customer/Home/MyOrders)");

                return new AIOrderTrackingResponse
                {
                    Found = true,
                    Orders = orderInfos,
                    Message = messageBuilder.ToString(),
                    QuickReplies = orderInfos.Take(3).Select(o => new QuickReplyButton
                    {
                        Text = $"📦 {o.OrderNumber[^8..]}",
                        Action = "send_message",
                        Payload = $"order {o.OrderNumber} track koro",
                        Style = o.Status == "Shipped" ? "warning" : "default"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders for user {UserId}", userId);
                return new AIOrderTrackingResponse
                {
                    Found = false,
                    Message = "অর্ডার দেখতে সমস্যা হয়েছে। আবার চেষ্টা করুন।"
                };
            }
        }

        private AIOrderInfo MapToOrderInfo(Order order)
        {
            var canCancel = order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed;
            var canReturn = order.Status == OrderStatus.Delivered &&
                            order.DeliveredAt.HasValue &&
                            (DateTime.UtcNow - order.DeliveredAt.Value).TotalDays <= 7;

            return new AIOrderInfo
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber ?? order.Id.ToString(),
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                StatusBangla = GetStatusBangla(order.Status),
                PaymentStatus = order.PaymentStatus.ToString(),
                DeliveryStatus = order.DeliveryStatus.ToString(),
                TotalAmount = order.TotalAmount,
                TrackingNumber = order.TrackingNumber,
                CourierName = order.CourierName,
                DeliveredAt = order.DeliveredAt,
                OrderUrl = $"/Customer/Home/OrderDetails/{order.Id}",
                CanCancel = canCancel,
                CanReturn = canReturn,
                Items = order.OrderItems?.Select(oi => new AIOrderItemInfo
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown",
                    Quantity = oi.Quantity,
                    Price = oi.UnitPrice,
                    ImageUrl = oi.Product?.ImageUrl
                }).ToList()
            };
        }

        private string GetStatusBangla(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "অপেক্ষমাণ",
            OrderStatus.Confirmed => "নিশ্চিত হয়েছে",
            OrderStatus.Processing => "প্রক্রিয়াধীন",
            OrderStatus.Shipped => "শিপ হয়েছে",
            OrderStatus.OutForDelivery => "ডেলিভারির পথে",
            OrderStatus.Delivered => "ডেলিভার হয়েছে",
            OrderStatus.Cancelled => "বাতিল",
            OrderStatus.Returned => "রিটার্ন হয়েছে",
            OrderStatus.Refunded => "রিফান্ড হয়েছে",
            _ => status.ToString()
        };

        private string GetOrderStatusMessage(Order order) => order.Status switch
        {
            OrderStatus.Pending => "⏳ আপনার অর্ডার পেয়েছি! শীঘ্রই কনফার্ম করা হবে।",
            OrderStatus.Confirmed => "✅ অর্ডার কনফার্ম হয়েছে! প্যাকিং এর জন্য প্রস্তুত।",
            OrderStatus.Processing => "📦 অর্ডার প্যাক করা হচ্ছে। কিছুক্ষণের মধ্যে শিপ হবে!",
            OrderStatus.Shipped => "🚚 আপনার অর্ডার পাঠানো হয়েছে! শীঘ্রই পৌঁছে যাবে।",
            OrderStatus.OutForDelivery => "🏃 ডেলিভারি বয় আপনার কাছে আসছে! ফোন রেডি রাখুন।",
            OrderStatus.Delivered => "🎉 অর্ডার সফলভাবে ডেলিভার হয়েছে! ধন্যবাদ!",
            OrderStatus.Cancelled => "❌ অর্ডার বাতিল করা হয়েছে।",
            OrderStatus.Returned => "↩️ পণ্য রিটার্ন হয়েছে। রিফান্ড প্রক্রিয়া চলছে।",
            OrderStatus.Refunded => "💰 রিফান্ড সম্পন্ন হয়েছে!",
            _ => ""
        };

        #endregion

        #region Advanced Features - Wishlist Management

        private async Task<AIChatResponse> HandleWishlistAddIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse
                {
                    Message = "💝 উইশলিস্টে যোগ করতে লগইন করুন!\n\n🔗 [লগইন করুন](/Identity/Account/Login)",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🔐 লগইন", Action = "open_url", Payload = "/Identity/Account/Login", Style = "primary" }
                    }
                };
            }

            // Try to extract product from context or message
            var productId = await ExtractProductIdFromMessageAsync(normalizedMessage, originalMessage, context);
            if (productId == null)
            {
                return new AIChatResponse
                {
                    Message = "💝 কোন পণ্যটি উইশলিস্টে যোগ করতে চান?\n\n" +
                              "পণ্যের নাম বলুন অথবা প্রোডাক্ট পেজ থেকে ❤️ বাটনে ক্লিক করুন।",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🛍️ পণ্য খুঁজুন", Action = "send_message", Payload = "পণ্য দেখাও", Style = "primary" },
                        new() { Text = "💝 উইশলিস্ট দেখুন", Action = "send_message", Payload = "amar wishlist dekhao", Style = "default" }
                    }
                };
            }

            var result = await AddToWishlistAsync(productId.Value, userId);
            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies
            };
        }

        private async Task<AIChatResponse> HandleWishlistViewIntentAsync(ConversationContext context, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse
                {
                    Message = "💝 উইশলিস্ট দেখতে লগইন করুন!\n\n🔗 [লগইন করুন](/Identity/Account/Login)",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🔐 লগইন", Action = "open_url", Payload = "/Identity/Account/Login", Style = "primary" }
                    }
                };
            }

            var result = await GetWishlistAsync(userId);

            // Update context with first wishlist product for future reference
            if (result.Items?.Any() == true)
            {
                var firstItem = result.Items.First();
                context.LastMentionedProductId = firstItem.ProductId;
                context.LastProductQuery = firstItem.ProductName;
            }

            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies
            };
        }

        private async Task<AIChatResponse> HandleWishlistRemoveIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse { Message = "লগইন করুন প্রথমে!" };
            }

            var productId = await ExtractProductIdFromMessageAsync(normalizedMessage, originalMessage, context);
            if (productId == null)
            {
                return new AIChatResponse
                {
                    Message = "কোন পণ্যটি উইশলিস্ট থেকে সরাতে চান?",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "💝 উইশলিস্ট দেখুন", Action = "send_message", Payload = "amar wishlist", Style = "primary" }
                    }
                };
            }

            var result = await RemoveFromWishlistAsync(productId.Value, userId);
            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies
            };
        }

        private async Task<AIChatResponse> HandleWishlistToCartIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse { Message = "লগইন করুন প্রথমে!" };
            }

            var productId = await ExtractProductIdFromMessageAsync(normalizedMessage, originalMessage, context);
            if (productId == null)
            {
                // Show wishlist and let user choose
                var wishlist = await GetWishlistAsync(userId);
                return new AIChatResponse
                {
                    Message = "কোন পণ্যটি কার্টে নিতে চান?\n\n" + wishlist.Message,
                    QuickReplies = wishlist.QuickReplies
                };
            }

            var result = await MoveWishlistToCartAsync(productId.Value, userId);
            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies
            };
        }

        public async Task<AIWishlistResponse> AddToWishlistAsync(int productId, string userId)
        {
            try
            {
                var product = await _db.Products.FindAsync(productId);
                if (product == null)
                {
                    return new AIWishlistResponse
                    {
                        Success = false,
                        Message = "পণ্য খুঁজে পাওয়া যায়নি!"
                    };
                }

                // Check if already in wishlist
                var existing = await _db.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existing != null)
                {
                    return new AIWishlistResponse
                    {
                        Success = true,
                        Message = $"💝 **{product.Name}** ইতিমধ্যে আপনার উইশলিস্টে আছে!",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "💝 উইশলিস্ট দেখুন", Action = "open_url", Payload = "/Customer/Wishlist", Style = "primary" },
                            new() { Text = "🛒 কার্টে নিন", Action = "send_message", Payload = $"{product.Name} cart e add koro", Style = "success" }
                        }
                    };
                }

                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    PriceWhenAdded = product.EffectivePrice,
                    AddedAt = DateTime.UtcNow
                };

                _db.Wishlists.Add(wishlistItem);
                await _db.SaveChangesAsync();

                var totalItems = await _db.Wishlists.CountAsync(w => w.UserId == userId);

                return new AIWishlistResponse
                {
                    Success = true,
                    TotalItems = totalItems,
                    Message = $"💝 **{product.Name}** উইশলিস্টে যোগ হয়েছে!\n\n" +
                              $"আপনার উইশলিস্টে এখন {totalItems}টি পণ্য আছে।",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "💝 উইশলিস্ট দেখুন", Action = "open_url", Payload = "/Customer/Wishlist", Style = "primary" },
                        new() { Text = "🛒 এখনই কিনুন", Action = "send_message", Payload = $"{product.Name} kinbo", Style = "success" },
                        new() { Text = "🛍️ আরো দেখুন", Action = "send_message", Payload = "আরো পণ্য দেখাও", Style = "default" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to wishlist");
                return new AIWishlistResponse
                {
                    Success = false,
                    Message = "উইশলিস্টে যোগ করতে সমস্যা হয়েছে।"
                };
            }
        }

        public async Task<AIWishlistResponse> RemoveFromWishlistAsync(int productId, string userId)
        {
            try
            {
                var item = await _db.Wishlists
                    .Include(w => w.Product)
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (item == null)
                {
                    return new AIWishlistResponse
                    {
                        Success = false,
                        Message = "এই পণ্যটি আপনার উইশলিস্টে নেই!"
                    };
                }

                _db.Wishlists.Remove(item);
                await _db.SaveChangesAsync();

                return new AIWishlistResponse
                {
                    Success = true,
                    Message = $"✅ **{item.Product?.Name}** উইশলিস্ট থেকে সরানো হয়েছে!",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "💝 উইশলিস্ট দেখুন", Action = "send_message", Payload = "wishlist dekhao", Style = "primary" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from wishlist");
                return new AIWishlistResponse { Success = false, Message = "সমস্যা হয়েছে।" };
            }
        }

        public async Task<AIWishlistResponse> GetWishlistAsync(string userId)
        {
            try
            {
                var items = await _db.Wishlists
                    .Include(w => w.Product)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.AddedAt)
                    .ToListAsync();

                if (!items.Any())
                {
                    return new AIWishlistResponse
                    {
                        Success = true,
                        TotalItems = 0,
                        Message = "💝 আপনার উইশলিস্ট খালি!\n\n" +
                                  "পছন্দের পণ্যে ❤️ ক্লিক করে উইশলিস্টে যোগ করুন।",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🛍️ শপিং করুন", Action = "open_url", Payload = "/Customer/Home", Style = "primary" },
                            new() { Text = "🔥 বেস্ট সেলার", Action = "send_message", Payload = "best seller", Style = "success" }
                        }
                    };
                }

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"💝 **আপনার উইশলিস্ট ({items.Count}টি পণ্য):**\n");

                var wishlistItems = new List<AIWishlistItem>();

                foreach (var item in items.Take(5))
                {
                    var product = item.Product;
                    if (product == null) continue;

                    var priceDropped = product.EffectivePrice < item.PriceWhenAdded;
                    var priceIcon = priceDropped ? "📉" : "";
                    var stockIcon = product.Stock > 0 ? "✅" : "❌";

                    messageBuilder.AppendLine($"• **{product.Name}**");
                    messageBuilder.AppendLine($"  💰 ৳{product.EffectivePrice:N0} {priceIcon} | {stockIcon} {(product.Stock > 0 ? "In Stock" : "Out of Stock")}");

                    if (priceDropped)
                    {
                        var dropPercent = Math.Round((item.PriceWhenAdded - product.EffectivePrice) / item.PriceWhenAdded * 100, 1);
                        messageBuilder.AppendLine($"  🎉 **{dropPercent}% দাম কমেছে!**");
                    }
                    messageBuilder.AppendLine();

                    wishlistItems.Add(new AIWishlistItem
                    {
                        Id = item.Id,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        CurrentPrice = product.EffectivePrice,
                        PriceWhenAdded = item.PriceWhenAdded,
                        HasPriceDropped = priceDropped,
                        ImageUrl = product.ImageUrl,
                        ProductUrl = $"/Customer/Home/Details/{product.Slug ?? product.Id.ToString()}",
                        InStock = product.Stock > 0,
                        AddedAt = item.AddedAt
                    });
                }

                if (items.Count > 5)
                {
                    messageBuilder.AppendLine($"📌 আরো {items.Count - 5}টি পণ্য আছে...");
                }

                messageBuilder.AppendLine("\n🔗 [সম্পূর্ণ উইশলিস্ট দেখুন](/Customer/Wishlist)");

                return new AIWishlistResponse
                {
                    Success = true,
                    TotalItems = items.Count,
                    Items = wishlistItems,
                    Message = messageBuilder.ToString(),
                    QuickReplies = wishlistItems.Take(3).Select(w => new QuickReplyButton
                    {
                        Text = $"🛒 {w.ProductName[..Math.Min(w.ProductName.Length, 10)]}...",
                        Action = "send_message",
                        Payload = $"{w.ProductName} cart e add koro",
                        Style = w.HasPriceDropped ? "success" : "primary"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist");
                return new AIWishlistResponse { Success = false, Message = "উইশলিস্ট দেখতে সমস্যা হয়েছে।" };
            }
        }

        public async Task<AIWishlistResponse> MoveWishlistToCartAsync(int productId, string userId)
        {
            try
            {
                var wishlistItem = await _db.Wishlists
                    .Include(w => w.Product)
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem?.Product == null)
                {
                    return new AIWishlistResponse { Success = false, Message = "পণ্য উইশলিস্টে নেই!" };
                }

                // Add to cart
                var cartResult = await AddToCartViaChatAsync(productId, 1, userId);

                if (cartResult.Success)
                {
                    // Remove from wishlist
                    _db.Wishlists.Remove(wishlistItem);
                    await _db.SaveChangesAsync();

                    return new AIWishlistResponse
                    {
                        Success = true,
                        Message = $"✅ **{wishlistItem.Product.Name}** কার্টে যোগ হয়েছে এবং উইশলিস্ট থেকে সরানো হয়েছে!",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🛒 কার্ট দেখুন", Action = "open_url", Payload = "/Customer/Home/Cart", Style = "primary" },
                            new() { Text = "✅ চেকআউট", Action = "open_url", Payload = "/Customer/Home/Checkout", Style = "success" }
                        }
                    };
                }

                return new AIWishlistResponse { Success = false, Message = cartResult.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving wishlist to cart");
                return new AIWishlistResponse { Success = false, Message = "সমস্যা হয়েছে।" };
            }
        }

        private async Task<int?> ExtractProductIdFromMessageAsync(string normalizedMessage, string originalMessage, ConversationContext? context)
        {
            // Try to find product ID in message
            var idMatch = Regex.Match(originalMessage, @"product[_\s]?id[:\s]?(\d+)|#(\d+)", RegexOptions.IgnoreCase);
            if (idMatch.Success)
            {
                var id = idMatch.Groups[1].Success ? idMatch.Groups[1].Value : idMatch.Groups[2].Value;
                if (int.TryParse(id, out var productId))
                    return productId;
            }

            // Check for contextual references like "eta", "oi", "seta", "this", "that"
            var contextualReferences = new[] {
                "eta", "ota", "seta", "oi", "ei", "this", "that", "it",
                "এটা", "ওটা", "সেটা", "ঐ", "এই", "ওই", "আগের", "ager", "last", "previous",
                "nibo", "নিব", "nebo", "নেব", "chai", "চাই", "lagbe", "লাগবে"
            };

            var lowerMessage = normalizedMessage.ToLower();
            var isContextualReference = contextualReferences.Any(r => lowerMessage.Contains(r));

            // If contextual reference found and we have context, use LastMentionedProductId
            if (isContextualReference && context?.LastMentionedProductId != null)
            {
                return context.LastMentionedProductId;
            }

            // Try to find by name
            var productName = ExtractProductNameFromMessage(normalizedMessage, originalMessage);
            if (!string.IsNullOrEmpty(productName))
            {
                // First, try to match from MentionedProducts in context (prefer recent context)
                if (context?.MentionedProducts?.Any() == true)
                {
                    var contextMatch = context.MentionedProducts
                        .LastOrDefault(mp => mp.ToLower().Contains(productName.ToLower()) || productName.ToLower().Contains(mp.ToLower()));

                    if (!string.IsNullOrEmpty(contextMatch))
                    {
                        // Search for this specific product from context
                        var contextProduct = await _db.Products
                            .Where(p => p.Status == ProductStatus.Active && p.Name.ToLower().Contains(contextMatch.ToLower()))
                            .FirstOrDefaultAsync();
                        if (contextProduct != null)
                            return contextProduct.Id;
                    }
                }

                // Fall back to database search
                var product = await _db.Products
                    .Where(p => p.Status == ProductStatus.Active &&
                           (p.Name.Contains(productName) || p.Name.ToLower().Contains(productName.ToLower())))
                    .FirstOrDefaultAsync();
                return product?.Id;
            }

            // Final fallback: use context LastMentionedProductId if available
            if (context?.LastMentionedProductId != null)
                return context.LastMentionedProductId;

            return null;
        }

        private string? ExtractProductNameFromMessage(string normalizedMessage, string originalMessage)
        {
            // Try to extract product name patterns
            var patterns = new[]
            {
                @"(?:এই|ei|eta|এটা)\s+(.+?)\s+(?:ta|টা)?",
                @"(.+?)\s+(?:add|save|wishlist|cart)",
                @"""(.+?)""",
                @"'(.+?)'"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(originalMessage, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups[1].Length > 2)
                    return match.Groups[1].Value.Trim();
            }

            return null;
        }

        #endregion

        #region Advanced Features - Coupon Discovery

        private async Task<AIChatResponse> HandleFindCouponIntentAsync(ConversationContext context, string? userId)
        {
            var result = await FindAvailableCouponsAsync(userId);

            // Save first coupon code to context if available
            if (result.AvailableCoupons?.Any() == true)
            {
                context.LastCouponCode = result.AvailableCoupons.First().Code;
            }

            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies,
                DetectedIntent = "find_coupon"
            };
        }

        private async Task<AIChatResponse> HandleApplyCouponIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context, string? userId)
        {
            // Extract coupon code from message
            var codeMatch = Regex.Match(originalMessage, @"(?:code|কোড|coupon|কুপন)[:\s]*([A-Z0-9]+)", RegexOptions.IgnoreCase);
            var couponCode = codeMatch.Success ? codeMatch.Groups[1].Value : null;

            if (string.IsNullOrEmpty(couponCode))
            {
                // Try to find any uppercase code
                codeMatch = Regex.Match(originalMessage, @"\b([A-Z0-9]{4,15})\b");
                couponCode = codeMatch.Success ? codeMatch.Groups[1].Value : null;
            }

            // Try from context if still no code
            if (string.IsNullOrEmpty(couponCode) && !string.IsNullOrEmpty(context.LastCouponCode))
            {
                couponCode = context.LastCouponCode;
            }

            if (string.IsNullOrEmpty(couponCode))
            {
                return new AIChatResponse
                {
                    Message = "🎟️ কুপন কোড দিন! যেমন: **SAVE20** বা **WELCOME10**",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🎁 কুপন খুঁজুন", Action = "send_message", Payload = "ki coupon ase", Style = "primary" }
                    }
                };
            }

            // Save to context
            context.LastCouponCode = couponCode;

            // Get cart total
            decimal cartTotal = 0;
            if (!string.IsNullOrEmpty(userId))
            {
                var cartItems = await _cartService.GetCartItemsAsync(userId);
                cartTotal = cartItems.Sum(c => c.EffectivePrice * c.Quantity);
            }

            var result = await ValidateCouponAsync(couponCode, userId, cartTotal);
            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies
            };
        }

        public async Task<AICouponResponse> FindAvailableCouponsAsync(string? userId, decimal? cartTotal = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var coupons = await _db.Coupons
                    .Include(c => c.Category)
                    .Where(c => c.IsActive &&
                               (c.StartDate == null || c.StartDate <= now) &&
                               (c.EndDate == null || c.EndDate >= now) &&
                               (c.UsageLimit == null || c.TimesUsed < c.UsageLimit) &&
                               // Filter: Public coupons (no user assigned) OR assigned to current user
                               (c.AssignedToUserId == null || c.AssignedToUserId == userId))
                    .OrderByDescending(c => c.DiscountValue)
                    .Take(10)
                    .ToListAsync();

                if (!coupons.Any())
                {
                    return new AICouponResponse
                    {
                        Success = false,
                        Message = "😕 দুঃখিত! এই মুহূর্তে কোনো অ্যাক্টিভ কুপন নেই।\n\n" +
                                  "তবে চিন্তা করবেন না - আমাদের পণ্যে সবসময় সেরা দাম দেওয়া হয়! 🛍️",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🔥 অফার দেখুন", Action = "send_message", Payload = "discount product dekhao", Style = "success" },
                            new() { Text = "🛍️ শপিং করুন", Action = "open_url", Payload = "/Customer/Home", Style = "primary" }
                        }
                    };
                }

                // Check user's first order status
                var isFirstOrder = true;
                if (!string.IsNullOrEmpty(userId))
                {
                    isFirstOrder = !await _db.Orders.AnyAsync(o => o.UserId == userId && o.Status == OrderStatus.Delivered);
                }

                var couponInfos = coupons.Select(c => new AICouponInfo
                {
                    Id = c.Id,
                    Code = c.Code,
                    Description = c.Description ?? "",
                    DiscountText = c.DiscountType == DiscountType.Percentage
                        ? $"{c.DiscountValue}% ছাড়"
                        : $"৳{c.DiscountValue} ছাড়",
                    MinimumOrder = c.MinimumOrderAmount,
                    MaxDiscount = c.MaximumDiscountAmount,
                    ExpiresAt = c.EndDate,
                    IsFirstOrderOnly = c.IsFirstOrderOnly,
                    CategoryRestriction = c.Category?.Name,
                    IsApplicable = (!c.IsFirstOrderOnly || isFirstOrder) &&
                                  (cartTotal == null || c.MinimumOrderAmount == null || cartTotal >= c.MinimumOrderAmount)
                }).ToList();

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("🎁 **উপলব্ধ কুপন কোড:**\n");

                foreach (var coupon in couponInfos.Take(5))
                {
                    var applicableIcon = coupon.IsApplicable ? "✅" : "⚠️";
                    messageBuilder.AppendLine($"{applicableIcon} **{coupon.Code}** - {coupon.DiscountText}");

                    if (coupon.MinimumOrder.HasValue)
                        messageBuilder.AppendLine($"   📦 মিনিমাম অর্ডার: ৳{coupon.MinimumOrder:N0}");

                    if (coupon.IsFirstOrderOnly)
                        messageBuilder.AppendLine($"   🆕 শুধুমাত্র প্রথম অর্ডারে");

                    if (coupon.ExpiresAt.HasValue)
                        messageBuilder.AppendLine($"   ⏰ মেয়াদ: {coupon.ExpiresAt:dd MMM yyyy}");

                    messageBuilder.AppendLine();
                }

                messageBuilder.AppendLine("💡 চেকআউটে কুপন কোড ব্যবহার করুন!");

                return new AICouponResponse
                {
                    Success = true,
                    AvailableCoupons = couponInfos,
                    Message = messageBuilder.ToString(),
                    QuickReplies = couponInfos.Where(c => c.IsApplicable).Take(3).Select(c => new QuickReplyButton
                    {
                        Text = $"🎟️ {c.Code}",
                        Action = "send_message",
                        Payload = $"coupon {c.Code} apply koro",
                        Style = "success"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding coupons");
                return new AICouponResponse { Success = false, Message = "কুপন খুঁজতে সমস্যা হয়েছে।" };
            }
        }

        public async Task<AICouponResponse> ValidateCouponAsync(string couponCode, string? userId, decimal cartTotal)
        {
            try
            {
                var coupon = await _db.Coupons
                    .Include(c => c.Category)
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper());

                if (coupon == null)
                {
                    return new AICouponResponse
                    {
                        Success = false,
                        Message = $"❌ **{couponCode}** কুপন কোড ভুল বা মেয়াদোত্তীর্ণ!\n\n" +
                                  "সঠিক কোড ব্যবহার করুন অথবা উপলব্ধ কুপন দেখুন।",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🎁 কুপন দেখুন", Action = "send_message", Payload = "ki coupon ase", Style = "primary" }
                        }
                    };
                }

                // Validate coupon
                var validationErrors = new List<string>();

                if (!coupon.IsActive)
                    validationErrors.Add("এই কুপন নিষ্ক্রিয়");

                if (coupon.StartDate.HasValue && coupon.StartDate > DateTime.UtcNow)
                    validationErrors.Add($"এই কুপন {coupon.StartDate:dd MMM} থেকে কার্যকর হবে");

                if (coupon.EndDate.HasValue && coupon.EndDate < DateTime.UtcNow)
                    validationErrors.Add("এই কুপনের মেয়াদ শেষ হয়ে গেছে");

                if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit)
                    validationErrors.Add("এই কুপনের ব্যবহার সীমা শেষ");

                if (coupon.MinimumOrderAmount.HasValue && cartTotal < coupon.MinimumOrderAmount)
                    validationErrors.Add($"মিনিমাম অর্ডার ৳{coupon.MinimumOrderAmount:N0} হতে হবে (বর্তমান: ৳{cartTotal:N0})");

                if (coupon.IsFirstOrderOnly && !string.IsNullOrEmpty(userId))
                {
                    var hasOrders = await _db.Orders.AnyAsync(o => o.UserId == userId && o.Status == OrderStatus.Delivered);
                    if (hasOrders)
                        validationErrors.Add("এই কুপন শুধুমাত্র প্রথম অর্ডারের জন্য");
                }

                if (validationErrors.Any())
                {
                    return new AICouponResponse
                    {
                        Success = false,
                        Message = $"⚠️ **{coupon.Code}** কুপন ব্যবহার করা যাচ্ছে না:\n\n" +
                                  string.Join("\n", validationErrors.Select(e => $"• {e}")),
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🎁 অন্য কুপন", Action = "send_message", Payload = "ki coupon ase", Style = "primary" }
                        }
                    };
                }

                // Calculate discount
                var discount = coupon.DiscountType == DiscountType.Percentage
                    ? cartTotal * (coupon.DiscountValue / 100)
                    : coupon.DiscountValue;

                if (coupon.MaximumDiscountAmount.HasValue)
                    discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);

                return new AICouponResponse
                {
                    Success = true,
                    AppliedCoupon = new AICouponInfo
                    {
                        Id = coupon.Id,
                        Code = coupon.Code,
                        DiscountText = coupon.DiscountType == DiscountType.Percentage
                            ? $"{coupon.DiscountValue}%"
                            : $"৳{coupon.DiscountValue}",
                        IsApplicable = true
                    },
                    DiscountAmount = discount,
                    Message = $"✅ **{coupon.Code}** কুপন ব্যবহারযোগ্য!\n\n" +
                              $"💰 ছাড়: **৳{discount:N0}**\n" +
                              $"🛒 কার্ট: ৳{cartTotal:N0}\n" +
                              $"💵 পেমেন্ট: **৳{(cartTotal - discount):N0}**\n\n" +
                              "চেকআউটে এই কোড ব্যবহার করুন! 🎉",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "✅ চেকআউট", Action = "open_url", Payload = "/Customer/Home/Checkout", Style = "success" },
                        new() { Text = "🛒 কার্ট দেখুন", Action = "open_url", Payload = "/Customer/Home/Cart", Style = "primary" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon {CouponCode}", couponCode);
                return new AICouponResponse { Success = false, Message = "কুপন চেক করতে সমস্যা হয়েছে।" };
            }
        }

        #endregion

        #region Advanced Features - Return & Refund

        private async Task<AIChatResponse> HandleReturnRequestIntentAsync(string normalizedMessage, string originalMessage, ConversationContext? context, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse
                {
                    Message = "রিটার্ন রিকোয়েস্ট করতে লগইন করুন!\n\n🔗 [লগইন করুন](/Identity/Account/Login)"
                };
            }

            // Try to extract order number from message, fallback to context
            var orderNumber = ExtractOrderNumber(originalMessage) ?? ExtractOrderNumber(normalizedMessage) ?? context?.LastOrderNumber;

            if (string.IsNullOrEmpty(orderNumber))
            {
                // Show recent deliveries that can be returned
                var recentOrders = await _db.Orders
                    .Where(o => o.UserId == userId &&
                               o.Status == OrderStatus.Delivered &&
                               o.DeliveredAt.HasValue &&
                               (DateTime.UtcNow - o.DeliveredAt.Value).TotalDays <= 7)
                    .OrderByDescending(o => o.DeliveredAt)
                    .Take(5)
                    .ToListAsync();

                if (!recentOrders.Any())
                {
                    return new AIChatResponse
                    {
                        Message = "😊 রিটার্ন করার মতো কোনো অর্ডার নেই।\n\n" +
                                  "রিটার্ন পলিসি অনুযায়ী ডেলিভারির ৭ দিনের মধ্যে রিটার্ন করা যায়।",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "📋 রিটার্ন পলিসি", Action = "send_message", Payload = "return policy", Style = "default" },
                            new() { Text = "📦 আমার অর্ডার", Action = "send_message", Payload = "amar sob order", Style = "primary" }
                        }
                    };
                }

                return new AIChatResponse
                {
                    Message = "↩️ **কোন অর্ডার রিটার্ন করতে চান?**\n\n" +
                              "রিটার্নযোগ্য অর্ডার:\n\n" +
                              string.Join("\n", recentOrders.Select(o =>
                                  $"• **{o.OrderNumber}** - ডেলিভার: {o.DeliveredAt:dd MMM} - ৳{o.TotalAmount:N0}")),
                    QuickReplies = recentOrders.Take(3).Select(o => new QuickReplyButton
                    {
                        Text = $"↩️ {o.OrderNumber?[^8..]}",
                        Action = "send_message",
                        Payload = $"order {o.OrderNumber} return korte chai",
                        Style = "warning"
                    }).ToList()
                };
            }

            // Save order number to context for future reference
            if (context != null)
            {
                context.LastOrderNumber = orderNumber;
            }

            var result = await CheckReturnEligibilityAsync(orderNumber, userId);
            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies
            };
        }

        private async Task<AIChatResponse> HandleRefundStatusIntentAsync(string normalizedMessage, string originalMessage, ConversationContext? context, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse { Message = "রিফান্ড স্ট্যাটাস দেখতে লগইন করুন!" };
            }

            // Try to extract order number from message, fallback to context
            var orderNumber = ExtractOrderNumber(originalMessage) ?? context?.LastOrderNumber;

            // Find orders with pending refunds
            var refundOrders = await _db.Orders
                .Where(o => o.UserId == userId &&
                           (o.Status == OrderStatus.Returned || o.Status == OrderStatus.Refunded ||
                            o.RefundStatus != RefundStatus.Pending))
                .OrderByDescending(o => o.ReturnedAt ?? o.UpdatedAt)
                .Take(5)
                .ToListAsync();

            if (!refundOrders.Any())
            {
                return new AIChatResponse
                {
                    Message = "😊 কোনো রিফান্ড প্রক্রিয়াধীন নেই!",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "↩️ রিটার্ন করুন", Action = "send_message", Payload = "return korte chai", Style = "warning" },
                        new() { Text = "📦 অর্ডার দেখুন", Action = "send_message", Payload = "amar order", Style = "primary" }
                    }
                };
            }

            // Save first refund order to context for future reference
            if (context != null && refundOrders.Any())
            {
                context.LastOrderNumber = refundOrders.First().OrderNumber;
            }

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("💰 **রিফান্ড স্ট্যাটাস:**\n");

            foreach (var order in refundOrders)
            {
                var statusIcon = order.RefundStatus switch
                {
                    RefundStatus.Completed => "✅",
                    RefundStatus.InProgress or RefundStatus.Approved => "⏳",
                    RefundStatus.Rejected => "❌",
                    _ => "📋"
                };

                messageBuilder.AppendLine($"{statusIcon} **{order.OrderNumber}**");
                messageBuilder.AppendLine($"   স্ট্যাটাস: {GetRefundStatusBangla(order.RefundStatus)}");
                if (order.ApprovedRefundAmount.HasValue)
                    messageBuilder.AppendLine($"   পরিমাণ: ৳{order.ApprovedRefundAmount:N0}");
                messageBuilder.AppendLine();
            }

            return new AIChatResponse
            {
                Message = messageBuilder.ToString(),
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "📞 হেল্পলাইন", Action = "send_message", Payload = "helpline", Style = "primary" },
                    new() { Text = "💬 এজেন্ট", Action = "send_message", Payload = "human agent", Style = "default" }
                }
            };
        }

        private string GetRefundStatusBangla(RefundStatus status) => status switch
        {
            RefundStatus.Pending => "অপেক্ষমাণ",
            RefundStatus.Reviewing => "পর্যালোচনা হচ্ছে",
            RefundStatus.InProgress => "প্রক্রিয়াধীন",
            RefundStatus.Approved => "অনুমোদিত",
            RefundStatus.Rejected => "প্রত্যাখ্যাত",
            RefundStatus.Completed => "সম্পন্ন",
            _ => status.ToString()
        };

        public async Task<AIReturnResponse> CheckReturnEligibilityAsync(string orderNumber, string userId)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => (o.OrderNumber == orderNumber || o.OrderNumber!.Contains(orderNumber)) &&
                                              o.UserId == userId);

                if (order == null)
                {
                    return new AIReturnResponse
                    {
                        IsEligible = false,
                        Message = $"❌ অর্ডার **{orderNumber}** খুঁজে পাওয়া যায়নি অথবা আপনার অর্ডার নয়।"
                    };
                }

                // Check eligibility
                if (order.Status != OrderStatus.Delivered)
                {
                    return new AIReturnResponse
                    {
                        IsEligible = false,
                        Message = $"⚠️ এই অর্ডার এখনো ডেলিভার হয়নি।\n\nবর্তমান স্ট্যাটাস: **{GetStatusBangla(order.Status)}**",
                        Order = MapToOrderInfo(order)
                    };
                }

                if (!order.DeliveredAt.HasValue)
                {
                    return new AIReturnResponse
                    {
                        IsEligible = false,
                        Message = "ডেলিভারি তারিখ পাওয়া যায়নি। হেল্পলাইনে যোগাযোগ করুন।"
                    };
                }

                var daysSinceDelivery = (DateTime.UtcNow - order.DeliveredAt.Value).TotalDays;
                var returnDays = 7; // Default return window

                if (daysSinceDelivery > returnDays)
                {
                    return new AIReturnResponse
                    {
                        IsEligible = false,
                        Message = $"❌ দুঃখিত! রিটার্নের সময়সীমা শেষ হয়ে গেছে।\n\n" +
                                  $"📅 ডেলিভার হয়েছিল: {order.DeliveredAt:dd MMM yyyy}\n" +
                                  $"⏰ রিটার্ন সময়সীমা ছিল: {returnDays} দিন\n\n" +
                                  "বিশেষ ক্ষেত্রে আমাদের হেল্পলাইনে যোগাযোগ করুন।",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "📞 হেল্পলাইন", Action = "send_message", Payload = "helpline", Style = "primary" }
                        }
                    };
                }

                var daysRemaining = (int)(returnDays - daysSinceDelivery);

                return new AIReturnResponse
                {
                    IsEligible = true,
                    Order = MapToOrderInfo(order),
                    DaysRemaining = daysRemaining,
                    Message = $"✅ **অর্ডার #{order.OrderNumber}** রিটার্ন করা যাবে!\n\n" +
                              $"📅 ডেলিভার হয়েছে: {order.DeliveredAt:dd MMM yyyy}\n" +
                              $"⏰ রিটার্নের জন্য বাকি আছে: **{daysRemaining} দিন**\n\n" +
                              $"🔗 [রিটার্ন রিকোয়েস্ট করুন](/Customer/Home/ReturnOrder/{order.Id})",
                    ReturnUrl = $"/Customer/Home/ReturnOrder/{order.Id}",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "↩️ রিটার্ন করুন", Action = "open_url", Payload = $"/Customer/Home/ReturnOrder/{order.Id}", Style = "warning" },
                        new() { Text = "📋 রিটার্ন পলিসি", Action = "send_message", Payload = "return policy", Style = "default" },
                        new() { Text = "📞 হেল্প", Action = "send_message", Payload = "helpline", Style = "primary" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking return eligibility for order {OrderNumber}", orderNumber);
                return new AIReturnResponse { IsEligible = false, Message = "সমস্যা হয়েছে। আবার চেষ্টা করুন।" };
            }
        }

        public async Task<AIChatResponse> GetReturnPolicyAsync()
        {
            var policy = await _siteSettingsService.GetPageContentBySlugAsync("return-policy");
            var message = "📋 **রিটার্ন ও রিফান্ড পলিসি:**\n\n" +
                          "✅ ডেলিভারির **৭ দিনের** মধ্যে রিটার্ন করতে পারবেন\n" +
                          "✅ পণ্য অক্ষত ও অব্যবহৃত থাকতে হবে\n" +
                          "✅ অরিজিনাল প্যাকেজিং সহ রিটার্ন করুন\n" +
                          "✅ রিফান্ড ৫-৭ কার্যদিবসের মধ্যে পাবেন\n\n" +
                          "❌ **রিটার্ন করা যাবে না:**\n" +
                          "• ব্যবহৃত পণ্য\n" +
                          "• কাস্টমাইজড পণ্য\n" +
                          "• ইনার ওয়্যার\n" +
                          "• বিউটি প্রোডাক্ট (খোলা হলে)\n\n" +
                          "🔗 [বিস্তারিত পলিসি দেখুন](/return-policy)";

            return new AIChatResponse
            {
                Message = message,
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "↩️ রিটার্ন করুন", Action = "send_message", Payload = "return korte chai", Style = "warning" },
                    new() { Text = "📞 হেল্পলাইন", Action = "send_message", Payload = "helpline", Style = "primary" }
                }
            };
        }

        #endregion

        #region Advanced Features - Product Comparison

        private async Task<AIChatResponse> HandleCompareProductsIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context)
        {
            // Try to extract product names/IDs from message
            var productIds = await ExtractMultipleProductIdsAsync(normalizedMessage, originalMessage, context);

            if (productIds.Count < 2)
            {
                // Not enough products - show recent products to compare
                return new AIChatResponse
                {
                    Message = "📊 **পণ্য তুলনা করুন!**\n\n" +
                              "দুই বা ততোধিক পণ্যের নাম বলুন তুলনা করতে।\n\n" +
                              "যেমন: \"শাড়ি ও থ্রি-পিস তুলনা করো\"\n" +
                              "অথবা: \"product #123 এবং #456 compare কর\"",
                    QuickReplies = context.MentionedProducts.Take(3).Select(p => new QuickReplyButton
                    {
                        Text = $"📊 {p[..Math.Min(p.Length, 10)]}...",
                        Action = "send_message",
                        Payload = $"{p} compare",
                        Style = "default"
                    }).ToList()
                };
            }

            var result = await CompareProductsAsync(productIds);
            return new AIChatResponse
            {
                Message = result.Message,
                QuickReplies = result.QuickReplies
            };
        }

        private async Task<List<int>> ExtractMultipleProductIdsAsync(string normalizedMessage, string originalMessage, ConversationContext? context)
        {
            var productIds = new List<int>();

            // Extract numeric IDs
            var idMatches = Regex.Matches(originalMessage, @"#?(\d{3,})");
            foreach (Match match in idMatches)
            {
                if (int.TryParse(match.Groups[1].Value, out var id))
                {
                    var exists = await _db.Products.AnyAsync(p => p.Id == id);
                    if (exists) productIds.Add(id);
                }
            }

            // Try to find products by names mentioned
            var productNames = ExtractProductNamesFromComparison(originalMessage);
            foreach (var name in productNames)
            {
                var product = await _db.Products
                    .Where(p => p.Name.Contains(name) && p.Status == ProductStatus.Active)
                    .FirstOrDefaultAsync();
                if (product != null && !productIds.Contains(product.Id))
                    productIds.Add(product.Id);
            }

            // Add from context if still not enough
            if (productIds.Count < 2 && context?.LastMentionedProductId != null)
            {
                if (!productIds.Contains(context.LastMentionedProductId.Value))
                    productIds.Add(context.LastMentionedProductId.Value);
            }

            return productIds;
        }

        private List<string> ExtractProductNamesFromComparison(string message)
        {
            var names = new List<string>();

            // Pattern: "X and Y", "X vs Y", "X এবং Y", etc.
            var patterns = new[]
            {
                @"(.+?)\s+(?:and|vs|versus|এবং|আর|ও)\s+(.+?)(?:\s+compare|\s+তুলনা|$)",
                @"compare\s+(.+?)\s+(?:and|with|vs)\s+(.+)",
                @"""(.+?)""\s+(?:and|vs)\s+""(.+?)"""
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (match.Groups[1].Length > 2) names.Add(match.Groups[1].Value.Trim());
                    if (match.Groups[2].Length > 2) names.Add(match.Groups[2].Value.Trim());
                    break;
                }
            }

            return names;
        }

        public async Task<AIComparisonResponse> CompareProductsAsync(List<int> productIds)
        {
            try
            {
                var products = await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Include(p => p.Reviews)
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                if (products.Count < 2)
                {
                    return new AIComparisonResponse
                    {
                        Success = false,
                        Message = "তুলনা করার জন্য কমপক্ষে দুটি পণ্য দরকার!"
                    };
                }

                var comparisonProducts = products.Select(p => new AIComparisonProduct
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    ImageUrl = p.ImageUrl,
                    ProductUrl = $"/Customer/Home/Details/{p.Slug ?? p.Id.ToString()}",
                    Rating = p.Seller?.Rating,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    InStock = p.Stock > 0,
                    Brand = p.Brand,
                    Seller = p.Seller?.ShopName,
                    Attributes = new Dictionary<string, string>
                    {
                        ["ক্যাটাগরি"] = p.Category?.Name ?? "N/A",
                        ["ফেব্রিক"] = p.Fabric ?? "N/A",
                        ["ব্র্যান্ড"] = p.Brand ?? "N/A",
                        ["ওয়ারেন্টি"] = p.WarrantyInfo ?? "N/A"
                    }
                }).ToList();

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("📊 **পণ্য তুলনা:**\n");

                // Build comparison table
                messageBuilder.AppendLine("| বৈশিষ্ট্য | " + string.Join(" | ", comparisonProducts.Select(p => p.Name[..Math.Min(p.Name.Length, 15)])) + " |");
                messageBuilder.AppendLine("|" + string.Join("|", Enumerable.Repeat("---", comparisonProducts.Count + 1)) + "|");

                // Price row
                messageBuilder.AppendLine("| 💰 দাম | " + string.Join(" | ", comparisonProducts.Select(p =>
                    p.DiscountPrice.HasValue ? $"~~৳{p.Price}~~ **৳{p.DiscountPrice}**" : $"৳{p.Price}")) + " |");

                // Rating row
                messageBuilder.AppendLine("| ⭐ রেটিং | " + string.Join(" | ", comparisonProducts.Select(p =>
                    p.Rating.HasValue ? $"{p.Rating:F1} ({p.ReviewCount})" : "N/A")) + " |");

                // Stock row
                messageBuilder.AppendLine("| 📦 স্টক | " + string.Join(" | ", comparisonProducts.Select(p =>
                    p.InStock ? "✅ আছে" : "❌ নেই")) + " |");

                // Seller row
                messageBuilder.AppendLine("| 🏪 সেলার | " + string.Join(" | ", comparisonProducts.Select(p => p.Seller ?? "N/A")) + " |");

                messageBuilder.AppendLine();

                // Recommendation
                var bestValue = comparisonProducts
                    .Where(p => p.InStock)
                    .OrderBy(p => p.DiscountPrice ?? p.Price)
                    .ThenByDescending(p => p.Rating ?? 0)
                    .FirstOrDefault();

                if (bestValue != null)
                {
                    messageBuilder.AppendLine($"💡 **সুপারিশ:** **{bestValue.Name}** - সেরা মূল্য ও গুণমান!");
                }

                return new AIComparisonResponse
                {
                    Success = true,
                    Products = comparisonProducts,
                    Message = messageBuilder.ToString(),
                    Recommendation = bestValue?.Name,
                    QuickReplies = comparisonProducts.Take(3).Select(p => new QuickReplyButton
                    {
                        Text = $"🛒 {p.Name[..Math.Min(p.Name.Length, 10)]}...",
                        Action = "send_message",
                        Payload = $"{p.Name} cart e add koro",
                        Style = p.Id == bestValue?.Id ? "success" : "primary"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing products");
                return new AIComparisonResponse { Success = false, Message = "তুলনা করতে সমস্যা হয়েছে।" };
            }
        }

        #endregion

        #region Advanced Features - Product Q&A

        private async Task<AIChatResponse> HandleProductQuestionIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context)
        {
            // Try to identify product
            var productId = await ExtractProductIdFromMessageAsync(normalizedMessage, originalMessage, context);

            if (productId == null)
            {
                return new AIChatResponse
                {
                    Message = "কোন পণ্য সম্পর্কে জানতে চান? পণ্যের নাম বলুন!",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🛍️ পণ্য খুঁজুন", Action = "send_message", Payload = "product search", Style = "primary" }
                    }
                };
            }

            // Extract question
            var question = ExtractQuestionFromMessage(normalizedMessage, originalMessage);

            return await AnswerProductQuestionAsync(productId.Value, question);
        }

        private string ExtractQuestionFromMessage(string normalizedMessage, string originalMessage)
        {
            // Remove product identifiers and extract the actual question
            var question = Regex.Replace(originalMessage, @"(?:ei|এই|eta|এটা)\s+(?:product|প্রোডাক্ট|jinish|জিনিস)\s*(?:ta|টা)?", "", RegexOptions.IgnoreCase);
            question = Regex.Replace(question, @"#\d+", "");
            return question.Trim();
        }

        public async Task<AIChatResponse> AnswerProductQuestionAsync(int productId, string question)
        {
            try
            {
                var product = await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .Include(p => p.Seller)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return new AIChatResponse
                    {
                        Message = "পণ্য খুঁজে পাওয়া যায়নি!",
                        IsSuccessful = false
                    };
                }

                var normalizedQuestion = NormalizeBanglish(question.ToLower());

                // AI-based answer generation based on product data
                var answer = GenerateProductAnswer(product, normalizedQuestion);

                return new AIChatResponse
                {
                    Message = $"📦 **{product.Name}** সম্পর্কে:\n\n{answer}\n\n" +
                              $"🔗 [বিস্তারিত দেখুন](/Customer/Home/Details/{product.Slug ?? product.Id.ToString()})",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🛒 কার্টে নিন", Action = "send_message", Payload = $"{product.Name} cart e add koro", Style = "success" },
                        new() { Text = "📋 আরো প্রশ্ন", Action = "send_message", Payload = $"এই প্রোডাক্ট সম্পর্কে আরো জানতে চাই", Style = "default" },
                        new() { Text = "📊 রিভিউ দেখুন", Action = "send_message", Payload = $"{product.Name} review dekhao", Style = "primary" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering product question");
                return new AIChatResponse { Message = "উত্তর দিতে সমস্যা হয়েছে।", IsSuccessful = false };
            }
        }

        private string GenerateProductAnswer(Products product, string question)
        {
            // Material/Fabric questions
            if (ContainsAny(question, new[] { "material", "fabric", "কাপড়", "kapar", "ম্যাটেরিয়াল", "কি দিয়ে", "ki diye" }))
            {
                return !string.IsNullOrEmpty(product.Fabric)
                    ? $"এই পণ্যের ম্যাটেরিয়াল: **{product.Fabric}**"
                    : "ম্যাটেরিয়ালের তথ্য পণ্যের বিবরণে দেওয়া আছে।";
            }

            // Wash/Care questions
            if (ContainsAny(question, new[] { "wash", "ধোয়া", "dhoa", "care", "যত্ন", "jotno", "পরিষ্কার" }))
            {
                return !string.IsNullOrEmpty(product.CareInstructions)
                    ? $"পরিচর্যা নির্দেশনা:\n{product.CareInstructions}"
                    : "সাধারণত ঠান্ডা পানিতে হাতে ধুয়ে ছায়ায় শুকাতে হবে।";
            }

            // Size questions
            if (ContainsAny(question, new[] { "size", "সাইজ", "মাপ", "map", "fitting" }))
            {
                return "সাইজ চার্ট প্রোডাক্ট পেজে দেওয়া আছে। আপনার মাপ অনুযায়ী সাইজ বেছে নিন।";
            }

            // Color questions
            if (ContainsAny(question, new[] { "color", "রঙ", "rong", "কালার" }))
            {
                return "উপলব্ধ রঙের অপশন প্রোডাক্ট পেজে দেখতে পাবেন।";
            }

            // Warranty questions
            if (ContainsAny(question, new[] { "warranty", "গ্যারান্টি", "garantee", "ওয়ারেন্টি" }))
            {
                return !string.IsNullOrEmpty(product.WarrantyInfo)
                    ? $"ওয়ারেন্টি তথ্য: {product.WarrantyInfo}"
                    : "এই পণ্যে নির্মাতা ওয়ারেন্টি আছে। বিস্তারিত প্রোডাক্ট পেজে দেখুন।";
            }

            // Origin questions
            if (ContainsAny(question, new[] { "original", "আসল", "asol", "নকল", "nokol", "genuine" }))
            {
                return "✅ আমরা 100% অরিজিনাল পণ্য বিক্রি করি। নকল হলে পুরো টাকা ফেরত!";
            }

            // Delivery questions
            if (ContainsAny(question, new[] { "delivery", "ডেলিভারি", "কবে পাব", "kobe pabo", "কত দিন" }))
            {
                var deliveryDays = product.EstimatedDeliveryDays ?? "৩-৫ কার্যদিবস";
                return $"🚚 আনুমানিক ডেলিভারি সময়: {deliveryDays} (ঢাকায়)\n\nঢাকার বাইরে ৫-৭ কার্যদিবস লাগতে পারে।";
            }

            // Return questions
            if (ContainsAny(question, new[] { "return", "রিটার্ন", "ফেরত", "ferot" }))
            {
                return product.IsReturnable
                    ? $"↩️ এই পণ্য ডেলিভারির {product.ReturnDays} দিনের মধ্যে রিটার্ন করা যাবে।"
                    : "⚠️ এই পণ্য রিটার্নযোগ্য নয়।";
            }

            // Default - show product summary
            return $"**{product.Name}**\n\n" +
                   $"💰 দাম: ৳{product.EffectivePrice:N0}\n" +
                   $"📦 স্টক: {(product.Stock > 0 ? "আছে" : "নেই")}\n" +
                   $"🏪 সেলার: {product.Seller?.ShopName ?? "Bangaliyana"}\n\n" +
                   (!string.IsNullOrEmpty(product.ShortDescription) ? $"📝 {product.ShortDescription}" : "");
        }

        #endregion

        #region Advanced Features - Reorder & Suggestions

        private async Task<AIChatResponse> HandleReorderIntentAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new AIChatResponse
                {
                    Message = "🔄 আগের অর্ডার রিপিট করতে লগইন করুন!",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🔐 লগইন", Action = "open_url", Payload = "/Identity/Account/Login", Style = "primary" }
                    }
                };
            }

            return await GetReorderSuggestionsAsync(userId);
        }

        private async Task<AIChatResponse> HandleFrequentlyBoughtIntentAsync(string normalizedMessage, string originalMessage, ConversationContext context)
        {
            var productId = await ExtractProductIdFromMessageAsync(normalizedMessage, originalMessage, context);

            if (productId == null)
            {
                return new AIChatResponse
                {
                    Message = "কোন পণ্যের সাথে কি কিনবেন জানতে চান? পণ্যের নাম বলুন!",
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "🛍️ পণ্য খুঁজুন", Action = "send_message", Payload = "product search", Style = "primary" }
                    }
                };
            }

            return await GetFrequentlyBoughtTogetherAsync(productId.Value);
        }

        public async Task<AIChatResponse> GetReorderSuggestionsAsync(string userId)
        {
            try
            {
                // Get products from past orders
                var pastOrderProducts = await _db.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Order!.UserId == userId &&
                                oi.Order.Status == OrderStatus.Delivered &&
                                oi.Product!.Status == ProductStatus.Active)
                    .OrderByDescending(oi => oi.Order!.DeliveredAt)
                    .Select(oi => oi.Product)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                if (!pastOrderProducts.Any())
                {
                    return new AIChatResponse
                    {
                        Message = "😊 এখনো কোনো অর্ডার ডেলিভার হয়নি!\n\nআগের অর্ডার ডেলিভার হলে এখান থেকে সহজে রিঅর্ডার করতে পারবেন।",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = "🛍️ শপিং করুন", Action = "open_url", Payload = "/Customer/Home", Style = "primary" }
                        }
                    };
                }

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("🔄 **আবার কিনুন:**\n");
                messageBuilder.AppendLine("আপনার আগের অর্ডার থেকে:\n");

                foreach (var product in pastOrderProducts.Take(5))
                {
                    if (product == null) continue;
                    var stockStatus = product.Stock > 0 ? "✅" : "❌";
                    messageBuilder.AppendLine($"• {stockStatus} **{product.Name}** - ৳{product.EffectivePrice:N0}");
                }

                return new AIChatResponse
                {
                    Message = messageBuilder.ToString(),
                    ProductSuggestions = pastOrderProducts.Where(p => p != null).Take(5).Select(p => new AIProductSuggestion
                    {
                        Id = p!.Id,
                        Name = p.Name,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl,
                        Slug = p.Slug
                    }).ToList(),
                    QuickReplies = pastOrderProducts.Where(p => p?.Stock > 0).Take(3).Select(p => new QuickReplyButton
                    {
                        Text = $"🛒 {p!.Name[..Math.Min(p.Name.Length, 10)]}...",
                        Action = "send_message",
                        Payload = $"{p.Name} cart e add koro",
                        Style = "success"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reorder suggestions");
                return new AIChatResponse { Message = "সমস্যা হয়েছে।", IsSuccessful = false };
            }
        }

        public async Task<AIChatResponse> GetFrequentlyBoughtTogetherAsync(int productId)
        {
            try
            {
                var product = await _db.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return new AIChatResponse { Message = "পণ্য খুঁজে পাওয়া যায়নি!" };
                }

                // Find products frequently bought together (same orders)
                var relatedProducts = await _db.OrderItems
                    .Where(oi => _db.OrderItems
                        .Where(oi2 => oi2.ProductId == productId)
                        .Select(oi2 => oi2.OrderId)
                        .Contains(oi.OrderId) && oi.ProductId != productId)
                    .GroupBy(oi => oi.ProductId)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToListAsync();

                var products = await _db.Products
                    .Where(p => relatedProducts.Contains(p.Id) && p.Status == ProductStatus.Active)
                    .ToListAsync();

                // If no frequently bought together, suggest from same category
                if (!products.Any())
                {
                    products = await _db.Products
                        .Where(p => p.CategoryId == product.CategoryId &&
                                   p.Id != productId &&
                                   p.Status == ProductStatus.Active)
                        .OrderByDescending(p => p.SoldCount)
                        .Take(5)
                        .ToListAsync();
                }

                if (!products.Any())
                {
                    return new AIChatResponse
                    {
                        Message = $"**{product.Name}** এর সাথে ম্যাচিং পণ্য খুঁজছি...\n\nএই ক্যাটাগরির আরো পণ্য দেখুন!",
                        QuickReplies = new List<QuickReplyButton>
                        {
                            new() { Text = $"📂 {product.Category?.Name}", Action = "send_message", Payload = $"{product.Category?.Name} দেখাও", Style = "primary" }
                        }
                    };
                }

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"🛒 **{product.Name}** এর সাথে ভালো যায়:\n");

                foreach (var p in products.Take(4))
                {
                    messageBuilder.AppendLine($"• **{p.Name}** - ৳{p.EffectivePrice:N0}");
                }

                messageBuilder.AppendLine("\n💡 একসাথে কিনলে ডেলিভারি চার্জ সাশ্রয় হবে!");

                return new AIChatResponse
                {
                    Message = messageBuilder.ToString(),
                    CrossSellProducts = products.Take(4).Select(p => new AICrossSellProduct
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl,
                        Slug = p.Slug,
                        Reason = "একসাথে কিনলে ভালো!"
                    }).ToList(),
                    QuickReplies = products.Take(3).Select(p => new QuickReplyButton
                    {
                        Text = $"🛒 {p.Name[..Math.Min(p.Name.Length, 10)]}...",
                        Action = "send_message",
                        Payload = $"{p.Name} cart e add koro",
                        Style = "success"
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting frequently bought together");
                return new AIChatResponse { Message = "সমস্যা হয়েছে।", IsSuccessful = false };
            }
        }

        private bool ContainsAny(string text, string[] keywords)
        {
            return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region ============ CHAT HISTORY & PERSISTENCE ============

        public async Task<AIChatSessionInfo> StartSessionAsync(string? userId, string? guestSessionId, string? initialQuery = null)
        {
            try
            {
                var session = new AIChatSession
                {
                    SessionCode = $"CHAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    UserId = userId,
                    GuestSessionId = guestSessionId,
                    InitialQuery = initialQuery,
                    Status = AIChatSessionStatus.Active,
                    DetectedLanguage = initialQuery != null ? DetectLanguage(initialQuery) : DetectedLanguage.Banglish,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };

                _db.AIChatSessions.Add(session);
                await _db.SaveChangesAsync();

                // Log analytics event
                await RecordAnalyticsEventAsync("session_started", session.Id);

                return MapToSessionInfo(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting chat session");
                throw;
            }
        }

        public async Task<AIChatSessionInfo> GetOrCreateSessionAsync(string? userId, string? guestSessionId)
        {
            try
            {
                // Try to find existing active session
                var existingSession = await _db.AIChatSessions
                    .Where(s => (userId != null && s.UserId == userId) ||
                               (guestSessionId != null && s.GuestSessionId == guestSessionId))
                    .Where(s => s.Status == AIChatSessionStatus.Active)
                    .Where(s => s.LastActivityAt > DateTime.UtcNow.AddHours(-24)) // Within 24 hours
                    .OrderByDescending(s => s.LastActivityAt)
                    .FirstOrDefaultAsync();

                if (existingSession != null)
                {
                    existingSession.LastActivityAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    return MapToSessionInfo(existingSession);
                }

                // Create new session
                return await StartSessionAsync(userId, guestSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating session");
                throw;
            }
        }

        public async Task<ChatHistoryResponse> GetChatHistoryAsync(int sessionId, int page = 1, int pageSize = 50)
        {
            try
            {
                var query = _db.AIChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderByDescending(m => m.CreatedAt);

                var totalMessages = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalMessages / (double)pageSize);

                var messages = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .OrderBy(m => m.CreatedAt) // Reverse for display order
                    .Select(m => new AIChatMessageInfo
                    {
                        Id = m.Id,
                        SessionId = m.SessionId,
                        Content = m.Content,
                        Sender = m.Sender,
                        MessageType = m.MessageType,
                        RichContent = m.RichContent,
                        QuickReplies = m.QuickReplies,
                        DetectedIntent = m.DetectedIntent,
                        IntentConfidence = m.IntentConfidence,
                        Sentiment = m.Sentiment,
                        Language = m.Language,
                        ResponseTimeMs = m.ResponseTimeMs,
                        WasHelpful = m.WasHelpful,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                return new ChatHistoryResponse
                {
                    SessionId = sessionId,
                    Messages = messages,
                    TotalMessages = totalMessages,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    HasMore = page < totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat history for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<List<AIChatSessionInfo>> GetRecentSessionsAsync(string userId, int count = 10)
        {
            try
            {
                var sessions = await _db.AIChatSessions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.LastActivityAt)
                    .Take(count)
                    .Include(s => s.User)
                    .Include(s => s.AssignedAgent)
                    .ToListAsync();

                var sessionInfos = new List<AIChatSessionInfo>();
                foreach (var s in sessions)
                {
                    var lastMessage = await _db.AIChatMessages
                        .Where(m => m.SessionId == s.Id)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefaultAsync();

                    var info = MapToSessionInfo(s);
                    info.LastMessage = lastMessage?.Length > 50 ? lastMessage[..50] + "..." : lastMessage;
                    sessionInfos.Add(info);
                }

                return sessionInfos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent sessions for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AIChatMessageInfo> SaveMessageAsync(int sessionId, string content, ChatMessageSender sender,
            ChatMessageType messageType = ChatMessageType.Text, string? richContent = null, string? detectedIntent = null)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var language = DetectLanguage(content);

                var message = new AIChatMessage
                {
                    SessionId = sessionId,
                    Content = content,
                    Sender = sender,
                    MessageType = messageType,
                    RichContent = richContent,
                    DetectedIntent = detectedIntent,
                    Language = language,
                    CreatedAt = DateTime.UtcNow
                };

                _db.AIChatMessages.Add(message);

                // Update session
                var session = await _db.AIChatSessions.FindAsync(sessionId);
                if (session != null)
                {
                    session.LastActivityAt = DateTime.UtcNow;
                    session.TotalMessages++;
                    if (sender == ChatMessageSender.User)
                        session.UserMessages++;
                    else if (sender == ChatMessageSender.AI)
                        session.AIResponses++;

                    // Update detected language
                    if (sender == ChatMessageSender.User)
                        session.DetectedLanguage = language;
                }

                await _db.SaveChangesAsync();

                return new AIChatMessageInfo
                {
                    Id = message.Id,
                    SessionId = message.SessionId,
                    Content = message.Content,
                    Sender = message.Sender,
                    MessageType = message.MessageType,
                    RichContent = message.RichContent,
                    DetectedIntent = message.DetectedIntent,
                    Language = message.Language,
                    CreatedAt = message.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving message for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> EndSessionAsync(int sessionId, string? feedback = null, int? rating = null)
        {
            try
            {
                var session = await _db.AIChatSessions.FindAsync(sessionId);
                if (session == null) return false;

                session.Status = AIChatSessionStatus.Closed;
                session.ClosedAt = DateTime.UtcNow;
                session.Feedback = feedback;
                session.Rating = rating;

                await _db.SaveChangesAsync();
                await RecordAnalyticsEventAsync("session_ended", sessionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session {SessionId}", sessionId);
                return false;
            }
        }

        private AIChatSessionInfo MapToSessionInfo(AIChatSession session)
        {
            return new AIChatSessionInfo
            {
                Id = session.Id,
                SessionCode = session.SessionCode,
                UserId = session.UserId,
                GuestSessionId = session.GuestSessionId,
                UserName = session.User?.FullName ?? session.GuestName,
                Status = session.Status,
                DetectedLanguage = session.DetectedLanguage,
                TotalMessages = session.TotalMessages,
                CreatedAt = session.CreatedAt,
                LastActivityAt = session.LastActivityAt,
                IsHandoffRequested = session.Status == AIChatSessionStatus.HandoffRequested,
                AssignedAgentName = session.AssignedAgent?.FullName
            };
        }

        #endregion

        #region ============ HUMAN AGENT HANDOFF ============

        public async Task<HandoffRequestResponse> RequestHandoffAsync(int sessionId, string? reason = null)
        {
            try
            {
                var session = await _db.AIChatSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                {
                    return new HandoffRequestResponse
                    {
                        Success = false,
                        Message = "Session not found"
                    };
                }

                // Update session status
                session.Status = AIChatSessionStatus.HandoffRequested;
                session.HandoffRequestedAt = DateTime.UtcNow;
                session.HandoffReason = reason;

                // Create queue entry (UserId is null for guests)
                var queueEntry = new AIChatHandoffQueue
                {
                    SessionId = sessionId,
                    UserId = session.UserId,
                    Reason = reason,
                    Priority = DetermineHandoffPriority(session),
                    RequestedAt = DateTime.UtcNow
                };

                _db.AIChatHandoffQueues.Add(queueEntry);
                await _db.SaveChangesAsync();

                // Calculate queue position
                var queuePosition = await _db.AIChatHandoffQueues
                    .Where(q => !q.IsAssigned && !q.IsResolved)
                    .Where(q => q.RequestedAt < queueEntry.RequestedAt)
                    .CountAsync() + 1;

                var estimatedWait = queuePosition * 5; // Estimate 5 minutes per person

                // Save system message
                await SaveMessageAsync(sessionId,
                    $"আপনার অনুরোধ গ্রহণ করা হয়েছে। একজন সাপোর্ট এজেন্ট শীঘ্রই আপনার সাথে যোগাযোগ করবেন। " +
                    $"আপনার সারির অবস্থান: #{queuePosition}। আনুমানিক অপেক্ষার সময়: {estimatedWait} মিনিট।",
                    ChatMessageSender.System);

                return new HandoffRequestResponse
                {
                    Success = true,
                    Message = "আপনার অনুরোধ গ্রহণ করা হয়েছে।",
                    QueueId = queueEntry.Id,
                    QueuePosition = queuePosition,
                    EstimatedWaitMinutes = estimatedWait,
                    QuickReplies = new List<QuickReplyButton>
                    {
                        new() { Text = "অপেক্ষা করব", Action = "wait", Style = "primary" },
                        new() { Text = "বাতিল করুন", Action = "cancel_handoff", Style = "danger" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting handoff for session {SessionId}", sessionId);
                return new HandoffRequestResponse
                {
                    Success = false,
                    Message = "সমস্যা হয়েছে। আবার চেষ্টা করুন।"
                };
            }
        }

        private int DetermineHandoffPriority(AIChatSession session)
        {
            // Higher priority (lower number) for:
            // - Logged in users (vs guests)
            // - Users with high message count (frustrated)
            // - Users who waited long

            int priority = 5;

            if (session.UserId != null) priority--;
            if (session.TotalMessages > 10) priority--;
            if (session.TotalMessages > 20) priority--;

            return Math.Max(1, priority);
        }

        public async Task<List<HandoffQueueItem>> GetHandoffQueueAsync()
        {
            try
            {
                var queue = await _db.AIChatHandoffQueues
                    .Where(q => !q.IsResolved)
                    .Include(q => q.Session)
                    .ThenInclude(s => s.User)
                    .OrderBy(q => q.Priority)
                    .ThenBy(q => q.RequestedAt)
                    .ToListAsync();

                var result = new List<HandoffQueueItem>();
                foreach (var q in queue)
                {
                    var lastMessage = await _db.AIChatMessages
                        .Where(m => m.SessionId == q.SessionId && m.Sender == ChatMessageSender.User)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefaultAsync();

                    var intents = await _db.AIChatMessages
                        .Where(m => m.SessionId == q.SessionId && m.DetectedIntent != null)
                        .Select(m => m.DetectedIntent!)
                        .Distinct()
                        .Take(5)
                        .ToListAsync();

                    result.Add(new HandoffQueueItem
                    {
                        QueueId = q.Id,
                        SessionId = q.SessionId,
                        UserId = q.UserId,
                        UserName = q.Session?.User?.FullName ?? "Guest",
                        UserEmail = q.Session?.User?.Email,
                        Reason = q.Reason,
                        Priority = q.Priority,
                        RequestedAt = q.RequestedAt,
                        WaitTimeMinutes = (int)(DateTime.UtcNow - q.RequestedAt).TotalMinutes,
                        LastMessage = lastMessage,
                        TotalMessages = q.Session?.TotalMessages ?? 0,
                        DetectedIntents = intents
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting handoff queue");
                throw;
            }
        }

        public async Task<bool> AcceptHandoffAsync(int queueId, string agentId)
        {
            try
            {
                var queueEntry = await _db.AIChatHandoffQueues
                    .Include(q => q.Session)
                    .FirstOrDefaultAsync(q => q.Id == queueId);

                if (queueEntry == null || queueEntry.IsAssigned) return false;

                queueEntry.IsAssigned = true;
                queueEntry.AssignedAgentId = agentId;
                queueEntry.AssignedAt = DateTime.UtcNow;

                if (queueEntry.Session != null)
                {
                    queueEntry.Session.Status = AIChatSessionStatus.HandedOff;
                    queueEntry.Session.AssignedAgentId = agentId;
                    queueEntry.Session.HandoffAcceptedAt = DateTime.UtcNow;
                }

                // Send system message
                await SaveMessageAsync(queueEntry.SessionId,
                    "একজন সাপোর্ট এজেন্ট আপনার চ্যাটে যোগ দিয়েছেন। আপনাকে সাহায্য করতে পারবেন।",
                    ChatMessageSender.System);

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting handoff {QueueId}", queueId);
                return false;
            }
        }

        public async Task<bool> ResolveHandoffAsync(int queueId, string agentId, string? resolutionNotes = null)
        {
            try
            {
                var queueEntry = await _db.AIChatHandoffQueues
                    .Include(q => q.Session)
                    .FirstOrDefaultAsync(q => q.Id == queueId);

                if (queueEntry == null) return false;

                queueEntry.IsResolved = true;
                queueEntry.ResolvedAt = DateTime.UtcNow;
                queueEntry.ResolutionNotes = resolutionNotes;

                if (queueEntry.Session != null)
                {
                    queueEntry.Session.Status = AIChatSessionStatus.Resolved;
                    queueEntry.Session.ResolvedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving handoff {QueueId}", queueId);
                return false;
            }
        }

        #endregion

        #region ============ ANALYTICS & INSIGHTS ============

        public async Task<AIChatDailyAnalytics> GetDailyAnalyticsAsync(DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                var sessions = await _db.AIChatSessions
                    .Where(s => s.CreatedAt >= startOfDay && s.CreatedAt < endOfDay)
                    .ToListAsync();

                var messages = await _db.AIChatMessages
                    .Where(m => m.CreatedAt >= startOfDay && m.CreatedAt < endOfDay)
                    .ToListAsync();

                var topIntents = messages
                    .Where(m => !string.IsNullOrEmpty(m.DetectedIntent))
                    .GroupBy(m => m.DetectedIntent!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                var languageBreakdown = messages
                    .GroupBy(m => m.Language.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                var messagesWithResponseTime = messages.Where(m => m.ResponseTimeMs.HasValue).ToList();
                var sessionsWithRating = sessions.Where(s => s.Rating.HasValue).ToList();

                return new AIChatDailyAnalytics
                {
                    Date = date.Date,
                    TotalSessions = sessions.Count,
                    TotalMessages = messages.Count,
                    SessionsResolved = sessions.Count(s => s.Status == AIChatSessionStatus.Resolved),
                    SessionsHandedOff = sessions.Count(s => s.Status == AIChatSessionStatus.HandedOff),
                    AverageResponseTimeMs = messagesWithResponseTime.Any() ? messagesWithResponseTime.Average(m => m.ResponseTimeMs ?? 0) : 0,
                    AverageRating = sessionsWithRating.Any() ? sessionsWithRating.Average(s => s.Rating ?? 0) : 0,
                    TotalRatings = sessions.Count(s => s.Rating.HasValue),
                    TopIntents = topIntents,
                    LanguageBreakdown = languageBreakdown
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily analytics for {Date}", date);
                throw;
            }
        }

        public async Task<AIChatAnalyticsReport> GetAnalyticsReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sessions = await _db.AIChatSessions
                    .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                    .ToListAsync();

                var totalSessions = sessions.Count;
                var uniqueUsers = sessions.Select(s => s.UserId).Distinct().Count();
                var resolved = sessions.Count(s => s.Status == AIChatSessionStatus.Resolved);
                var handedOff = sessions.Count(s => s.Status == AIChatSessionStatus.HandedOff);

                var dailyBreakdown = new List<AIChatDailyAnalytics>();
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    dailyBreakdown.Add(await GetDailyAnalyticsAsync(date));
                }

                var sessionsWithRatings = sessions.Where(s => s.Rating.HasValue).ToList();

                return new AIChatAnalyticsReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalSessions = totalSessions,
                    TotalMessages = sessions.Sum(s => s.TotalMessages),
                    UniqueUsers = uniqueUsers,
                    ResolutionRate = totalSessions > 0 ? (double)resolved / totalSessions * 100 : 0,
                    HandoffRate = totalSessions > 0 ? (double)handedOff / totalSessions * 100 : 0,
                    AverageRating = sessionsWithRatings.Any() ? sessionsWithRatings.Average(s => s.Rating ?? 0) : 0,
                    DailyBreakdown = dailyBreakdown
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics report");
                throw;
            }
        }

        public async Task RecordAnalyticsEventAsync(string eventType, int sessionId, string? additionalData = null)
        {
            try
            {
                // Update daily analytics
                var today = DateTime.UtcNow.Date;
                var analytics = await _db.AIChatAnalytics
                    .FirstOrDefaultAsync(a => a.Date == today);

                if (analytics == null)
                {
                    analytics = new AIChatAnalytics
                    {
                        Date = today,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.AIChatAnalytics.Add(analytics);
                }

                switch (eventType)
                {
                    case "session_started":
                        analytics.TotalSessions++;
                        break;
                    case "cart_addition":
                        analytics.CartAdditions++;
                        break;
                    case "wishlist_addition":
                        analytics.WishlistAdditions++;
                        break;
                    case "product_searched":
                        analytics.ProductsSearched++;
                        break;
                    case "order_tracked":
                        analytics.OrdersTracked++;
                        break;
                }

                analytics.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording analytics event {EventType}", eventType);
            }
        }

        public async Task<List<IntentAnalytics>> GetTopIntentsAsync(DateTime startDate, DateTime endDate, int count = 10)
        {
            try
            {
                var intents = await _db.AIChatMessages
                    .Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate)
                    .Where(m => !string.IsNullOrEmpty(m.DetectedIntent))
                    .GroupBy(m => m.DetectedIntent!)
                    .Select(g => new
                    {
                        Intent = g.Key,
                        Count = g.Count(),
                        AvgConfidence = g.Average(m => m.IntentConfidence ?? 0)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(count)
                    .ToListAsync();

                var total = intents.Sum(i => i.Count);

                return intents.Select(i => new IntentAnalytics
                {
                    Intent = i.Intent,
                    IntentDisplayName = GetIntentDisplayName(i.Intent),
                    Count = i.Count,
                    Percentage = total > 0 ? (double)i.Count / total * 100 : 0,
                    AverageConfidence = i.AvgConfidence
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top intents");
                throw;
            }
        }

        private string GetIntentDisplayName(string intent)
        {
            var displayNames = new Dictionary<string, string>
            {
                ["search_product"] = "Product Search",
                ["add_to_cart"] = "Add to Cart",
                ["track_order"] = "Order Tracking",
                ["wishlist_view"] = "View Wishlist",
                ["find_coupon"] = "Coupon Discovery",
                ["return_request"] = "Return Request",
                ["compare_products"] = "Product Comparison",
                ["greeting"] = "Greeting",
                ["help"] = "Help Request"
            };

            return displayNames.TryGetValue(intent, out var name) ? name : intent;
        }

        #endregion

        #region ============ PERSONALIZATION ============

        public async Task<UserChatPreferences> GetUserPreferencesAsync(string userId)
        {
            try
            {
                var prefs = await _db.UserAIChatPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (prefs == null)
                {
                    return new UserChatPreferences();
                }

                return new UserChatPreferences
                {
                    PreferredLanguage = prefs.PreferredLanguage,
                    AutoDetectLanguage = prefs.AutoDetectLanguage,
                    EnableNotifications = prefs.EnableChatNotifications,
                    EnableSoundNotifications = prefs.EnableSoundNotifications,
                    EnablePersonalization = prefs.EnablePersonalizedResponses,
                    RememberHistory = prefs.RememberConversationHistory,
                    ShowRecommendations = prefs.ShowProductRecommendations,
                    InterestedCategories = !string.IsNullOrEmpty(prefs.InterestedCategories)
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(prefs.InterestedCategories)
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user preferences for {UserId}", userId);
                return new UserChatPreferences();
            }
        }

        public async Task<bool> UpdateUserPreferencesAsync(string userId, UserChatPreferences preferences)
        {
            try
            {
                var prefs = await _db.UserAIChatPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (prefs == null)
                {
                    prefs = new UserAIChatPreference
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.UserAIChatPreferences.Add(prefs);
                }

                prefs.PreferredLanguage = preferences.PreferredLanguage;
                prefs.AutoDetectLanguage = preferences.AutoDetectLanguage;
                prefs.EnableChatNotifications = preferences.EnableNotifications;
                prefs.EnableSoundNotifications = preferences.EnableSoundNotifications;
                prefs.EnablePersonalizedResponses = preferences.EnablePersonalization;
                prefs.RememberConversationHistory = preferences.RememberHistory;
                prefs.ShowProductRecommendations = preferences.ShowRecommendations;
                prefs.InterestedCategories = preferences.InterestedCategories != null
                    ? System.Text.Json.JsonSerializer.Serialize(preferences.InterestedCategories)
                    : null;
                prefs.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for {UserId}", userId);
                return false;
            }
        }

        public async Task<AIChatResponse> GetPersonalizedResponseAsync(string userMessage, int sessionId, string userId)
        {
            try
            {
                // Get user preferences
                var prefs = await GetUserPreferencesAsync(userId);

                // Get user's browsing/purchase history
                var recentProducts = await _db.ProductViews
                    .Where(v => v.UserId == userId)
                    .OrderByDescending(v => v.ViewedAt)
                    .Take(5)
                    .Select(v => v.Product!.Name)
                    .ToListAsync();

                var recentPurchases = await _db.OrderItems
                    .Where(oi => oi.Order!.UserId == userId && oi.Order.Status == OrderStatus.Delivered)
                    .OrderByDescending(oi => oi.Order!.OrderDate)
                    .Take(5)
                    .Select(oi => oi.Product!.Name)
                    .ToListAsync();

                // Generate response with personalization context
                var response = await GetResponseAsync(userMessage, sessionId, userId);

                // Add personalized touches based on history
                if (prefs.ShowRecommendations && recentProducts.Any())
                {
                    response.Message += $"\n\n💡 *আপনার সম্প্রতি দেখা:* {string.Join(", ", recentProducts.Take(3))}";
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized response");
                return await GetResponseAsync(userMessage, sessionId, userId);
            }
        }

        #endregion

        #region ============ MULTI-LANGUAGE ============

        public DetectedLanguage DetectLanguage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return DetectedLanguage.Unknown;

            // Count Bengali characters
            var bengaliCount = message.Count(c => c >= 0x0980 && c <= 0x09FF);
            var englishCount = message.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
            var totalChars = message.Length;

            var bengaliRatio = (double)bengaliCount / totalChars;
            var englishRatio = (double)englishCount / totalChars;

            if (bengaliRatio > 0.5)
                return DetectedLanguage.Bengali;
            if (englishRatio > 0.8)
                return DetectedLanguage.English;
            if (bengaliRatio > 0 || HasBanglishPatterns(message))
                return DetectedLanguage.Banglish;

            return DetectedLanguage.Unknown;
        }

        private bool HasBanglishPatterns(string message)
        {
            var banglishPatterns = new[]
            {
                "ami", "tumi", "apni", "kemon", "achen", "korte", "chai", "debe",
                "kothay", "keno", "ki", "kivabe", "kobe", "koto", "dibo", "nebo",
                "hobe", "korbo", "jabo", "asbo", "khabo", "dekhbo", "bolbo"
            };

            var lower = message.ToLower();
            return banglishPatterns.Any(p => lower.Contains(p));
        }

        public string FormatResponseInLanguage(string message, DetectedLanguage language)
        {
            // The current system already responds in Banglish/Bengali mix
            // This method can be extended for pure Bengali or English responses
            return message;
        }

        #endregion

        #region ============ PROACTIVE ENGAGEMENT ============

        public async Task<List<ProactiveTriggerInfo>> GetActiveTriggersAsync()
        {
            try
            {
                var triggers = await _db.ProactiveChatTriggers
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.Priority)
                    .ToListAsync();

                return triggers.Select(t => new ProactiveTriggerInfo
                {
                    Id = t.Id,
                    Name = t.Name,
                    TriggerType = t.TriggerType,
                    TriggerValue = t.TriggerValue,
                    PageUrlPattern = t.PageUrlPattern,
                    Message = t.Message,
                    MessageBengali = t.MessageBengali,
                    QuickReplies = !string.IsNullOrEmpty(t.QuickReplies)
                        ? System.Text.Json.JsonSerializer.Deserialize<List<QuickReplyButton>>(t.QuickReplies)
                        : null,
                    DelaySeconds = t.DelaySeconds
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active triggers");
                return new List<ProactiveTriggerInfo>();
            }
        }

        public async Task<ProactiveMessageResult?> CheckProactiveTriggerAsync(string? userId, string pageUrl, int timeOnPage, string? deviceType)
        {
            try
            {
                var triggers = await GetActiveTriggersAsync();

                foreach (var trigger in triggers)
                {
                    bool shouldTrigger = false;

                    switch (trigger.TriggerType)
                    {
                        case "TimeOnPage":
                            shouldTrigger = timeOnPage >= (trigger.TriggerValue ?? 30);
                            break;
                        case "PageView":
                            if (!string.IsNullOrEmpty(trigger.PageUrlPattern))
                            {
                                shouldTrigger = System.Text.RegularExpressions.Regex.IsMatch(
                                    pageUrl, trigger.PageUrlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            }
                            break;
                        case "CartAbandonment":
                            if (userId != null)
                            {
                                var hasCartItems = await _db.PersistentCarts.AnyAsync(c => c.UserId == userId);
                                shouldTrigger = hasCartItems && timeOnPage >= (trigger.TriggerValue ?? 60);
                            }
                            break;
                    }

                    if (shouldTrigger)
                    {
                        return new ProactiveMessageResult
                        {
                            ShouldShow = true,
                            TriggerId = trigger.Id,
                            Message = trigger.Message,
                            QuickReplies = trigger.QuickReplies,
                            DelayMs = trigger.DelaySeconds * 1000
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking proactive trigger");
                return null;
            }
        }

        public async Task RecordTriggerInteractionAsync(int triggerId, string interactionType)
        {
            try
            {
                var trigger = await _db.ProactiveChatTriggers.FindAsync(triggerId);
                if (trigger == null) return;

                switch (interactionType)
                {
                    case "triggered":
                        trigger.TimesTriggered++;
                        break;
                    case "clicked":
                        trigger.TimesClicked++;
                        break;
                    case "converted":
                        trigger.TimesConverted++;
                        break;
                }

                trigger.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording trigger interaction");
            }
        }

        #endregion

        #region ============ MESSAGE FEEDBACK ============

        public async Task<bool> SubmitMessageFeedbackAsync(int messageId, bool wasHelpful, string? comment = null)
        {
            try
            {
                var message = await _db.AIChatMessages.FindAsync(messageId);
                if (message == null) return false;

                message.WasHelpful = wasHelpful;
                message.UserFeedback = comment;

                // Update session feedback stats
                var session = await _db.AIChatSessions.FindAsync(message.SessionId);
                if (session != null)
                {
                    session.WasHelpful = wasHelpful;

                    // AI Learning: Track feedback for intent analysis
                    await TrackFeedbackForLearningAsync(message, session, wasHelpful, comment);
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting message feedback");
                return false;
            }
        }

        /// <summary>
        /// Track feedback for AI learning and improvement
        /// </summary>
        private async Task TrackFeedbackForLearningAsync(AIChatMessage message, AIChatSession session, bool wasHelpful, string? comment)
        {
            try
            {
                var intent = message.DetectedIntent ?? "unknown";

                // Log feedback for analysis
                _logger.LogInformation(
                    "AI Feedback Received - Intent: {Intent}, Helpful: {WasHelpful}, SessionId: {SessionId}, MessageId: {MessageId}",
                    intent,
                    wasHelpful,
                    session.Id,
                    message.Id
                );

                // Find or create learning record for this intent
                var learningRecord = await _db.AILearningRecords
                    .FirstOrDefaultAsync(r => r.Intent == intent && r.IsActive);

                if (learningRecord == null)
                {
                    // Create new learning record
                    learningRecord = new AILearningRecord
                    {
                        Intent = intent,
                        QueryPattern = session.InitialQuery?.Substring(0, Math.Min(500, session.InitialQuery?.Length ?? 0)),
                        OriginalResponse = message.Content?.Substring(0, Math.Min(2000, message.Content?.Length ?? 0)),
                        PositiveFeedbackCount = wasHelpful ? 1 : 0,
                        NegativeFeedbackCount = wasHelpful ? 0 : 1,
                        ConfidenceAdjustment = wasHelpful ? 0.01 : -0.02, // Negative feedback has more impact
                        IsActive = true
                    };
                    _db.AILearningRecords.Add(learningRecord);
                }
                else
                {
                    // Update existing record
                    if (wasHelpful)
                    {
                        learningRecord.PositiveFeedbackCount++;
                        learningRecord.ConfidenceAdjustment = Math.Min(1.0, learningRecord.ConfidenceAdjustment + 0.01);
                    }
                    else
                    {
                        learningRecord.NegativeFeedbackCount++;
                        learningRecord.ConfidenceAdjustment = Math.Max(-1.0, learningRecord.ConfidenceAdjustment - 0.02);
                    }
                    learningRecord.UpdatedAt = DateTime.UtcNow;
                }

                // Update daily feedback analysis
                var today = DateTime.UtcNow.Date;
                var analysis = await _db.AIFeedbackAnalyses
                    .FirstOrDefaultAsync(a => a.Intent == intent && a.AnalysisDate == today);

                if (analysis == null)
                {
                    analysis = new AIFeedbackAnalysis
                    {
                        Intent = intent,
                        AnalysisDate = today,
                        TotalResponses = 1,
                        PositiveCount = wasHelpful ? 1 : 0,
                        NegativeCount = wasHelpful ? 0 : 1,
                        SatisfactionRate = wasHelpful ? 100 : 0
                    };
                    _db.AIFeedbackAnalyses.Add(analysis);
                }
                else
                {
                    analysis.TotalResponses++;
                    if (wasHelpful) analysis.PositiveCount++;
                    else analysis.NegativeCount++;
                    analysis.SatisfactionRate = (double)analysis.PositiveCount / analysis.TotalResponses * 100;
                }

                await _db.SaveChangesAsync();

                // Log warning for consistent negative feedback
                if (learningRecord.NegativeFeedbackCount > learningRecord.PositiveFeedbackCount &&
                    learningRecord.NegativeFeedbackCount >= 3)
                {
                    _logger.LogWarning(
                        "AI Self-Learning Alert: Intent '{Intent}' has {NegCount} negative vs {PosCount} positive feedback. Needs improvement.",
                        intent,
                        learningRecord.NegativeFeedbackCount,
                        learningRecord.PositiveFeedbackCount
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking feedback for AI learning");
            }
        }

        /// <summary>
        /// Get learning adjustment for an intent based on feedback history
        /// </summary>
        public async Task<double> GetIntentConfidenceAdjustmentAsync(string intent)
        {
            try
            {
                var record = await _db.AILearningRecords
                    .Where(r => r.Intent == intent && r.IsActive)
                    .FirstOrDefaultAsync();

                return record?.ConfidenceAdjustment ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if an intent has too many negative feedbacks and needs alternative response
        /// </summary>
        public async Task<bool> ShouldUseAlternativeResponseAsync(string intent)
        {
            try
            {
                var record = await _db.AILearningRecords
                    .Where(r => r.Intent == intent && r.IsActive)
                    .FirstOrDefaultAsync();

                if (record == null) return false;

                // If negative feedback is significantly higher than positive, suggest alternative
                return record.NegativeFeedbackCount > record.PositiveFeedbackCount * 2 &&
                       record.NegativeFeedbackCount >= 5;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get feedback statistics for an intent
        /// </summary>
        public async Task<(int positive, int negative, double satisfactionRate)> GetIntentFeedbackStatsAsync(string intent)
        {
            try
            {
                var record = await _db.AILearningRecords
                    .Where(r => r.Intent == intent && r.IsActive)
                    .FirstOrDefaultAsync();

                if (record == null) return (0, 0, 100);

                var total = record.PositiveFeedbackCount + record.NegativeFeedbackCount;
                var rate = total > 0 ? (double)record.PositiveFeedbackCount / total * 100 : 100;

                return (record.PositiveFeedbackCount, record.NegativeFeedbackCount, rate);
            }
            catch
            {
                return (0, 0, 100);
            }
        }

        public async Task<FeedbackSummary> GetSessionFeedbackSummaryAsync(int sessionId)
        {
            try
            {
                var messages = await _db.AIChatMessages
                    .Where(m => m.SessionId == sessionId && m.WasHelpful.HasValue)
                    .ToListAsync();

                var positive = messages.Count(m => m.WasHelpful == true);
                var negative = messages.Count(m => m.WasHelpful == false);
                var total = positive + negative;

                return new FeedbackSummary
                {
                    SessionId = sessionId,
                    TotalFeedback = total,
                    PositiveCount = positive,
                    NegativeCount = negative,
                    SatisfactionRate = total > 0 ? (double)positive / total * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback summary");
                return new FeedbackSummary { SessionId = sessionId };
            }
        }

        #endregion

        #region Seller Support Features

        /// <summary>
        /// Seller-specific intent patterns
        /// </summary>
        private static readonly Dictionary<string, string[]> _sellerIntentPatterns = new()
        {
            ["seller_orders"] = new[] { "order", "অর্ডার", "orders", "pending order", "new order", "আজকের অর্ডার", "today order", "অর্ডার কত", "কতটা অর্ডার" },
            ["seller_sales"] = new[] { "sales", "সেলস", "বিক্রি", "revenue", "রেভিনিউ", "আয়", "income", "earning", "ইনকাম", "কত বিক্রি", "মাসিক বিক্রি", "daily sales" },
            ["seller_payment"] = new[] { "payment", "পেমেন্ট", "withdraw", "উইথড্র", "টাকা", "পাবো", "pending payment", "পাওনা", "balance", "ব্যালেন্স", "টাকা তুলতে" },
            ["seller_products"] = new[] { "product", "প্রোডাক্ট", "stock", "স্টক", "inventory", "low stock", "out of stock", "স্টক শেষ", "best seller", "বেস্ট সেলার" },
            ["seller_improve"] = new[] { "improve", "উন্নতি", "rating", "রেটিং", "tips", "টিপস", "suggestion", "পরামর্শ", "better", "ভালো করতে", "shop rating" },
            ["seller_messages"] = new[] { "message", "মেসেজ", "ticket", "টিকেট", "customer message", "inquiry", "জিজ্ঞাসা", "complaint", "অভিযোগ" },
            ["seller_dashboard"] = new[] { "dashboard", "ড্যাশবোর্ড", "summary", "সামারি", "overview", "সব দেখাও", "overall", "সব কিছু" }
        };

        public async Task<bool> IsSellerAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            return await _db.Sellers.AnyAsync(s => s.UserId == userId && s.IsVerified && s.Status == SellerStatus.Approved);
        }

        public async Task<string> GetSellerGreetingMessageAsync(string sellerName, string? shopName)
        {
            var hour = DateTime.Now.Hour;
            var greeting = hour switch
            {
                < 12 => "সুপ্রভাত",
                < 17 => "শুভ অপরাহ্ন",
                < 20 => "শুভ সন্ধ্যা",
                _ => "শুভ রাত্রি"
            };

            var shopGreeting = !string.IsNullOrEmpty(shopName) ? $" ({shopName})" : "";

            // Get quick summary
            var todayStart = DateTime.Today;
            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.ShopName == shopName);
            var todayOrders = 0;
            var pendingOrders = 0;

            if (seller != null)
            {
                todayOrders = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id && oi.Order!.OrderDate >= todayStart)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();

                pendingOrders = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id && oi.Order!.Status == OrderStatus.Pending)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();
            }

            var summaryPart = todayOrders > 0 || pendingOrders > 0
                ? $"\n\n📊 আজকের সারাংশ:\n• নতুন অর্ডার: {todayOrders}টি\n• পেন্ডিং অর্ডার: {pendingOrders}টি"
                : "";

            return $"{greeting}, {sellerName}{shopGreeting}! 🏪\n\n" +
                   $"আমি আপনার Seller Assistant। আপনার দোকান সংক্রান্ত যেকোনো তথ্য বা সাহায্যের জন্য আমাকে জিজ্ঞাসা করুন।{summaryPart}\n\n" +
                   "আমি সাহায্য করতে পারি:\n" +
                   "📦 অর্ডার ম্যানেজমেন্ট\n" +
                   "💰 সেলস ও রেভিনিউ\n" +
                   "💳 পেমেন্ট স্ট্যাটাস\n" +
                   "📊 প্রোডাক্ট ইনসাইটস\n" +
                   "⭐ শপ ইমপ্রুভমেন্ট টিপস\n\n" +
                   "কিভাবে সাহায্য করতে পারি?";
        }

        public async Task<AIChatResponse> GetSellerResponseAsync(string userMessage, int sessionId, string sellerId)
        {
            try
            {
                var seller = await _db.Sellers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == sellerId);

                if (seller == null)
                {
                    return new AIChatResponse
                    {
                        Message = "দুঃখিত, আপনার সেলার একাউন্ট খুঁজে পাওয়া যায়নি। অনুগ্রহ করে আবার লগইন করুন।",
                        IsSuccessful = false
                    };
                }

                var normalizedMessage = userMessage.ToLower().Trim();
                var intent = DetectSellerIntent(normalizedMessage);

                return intent switch
                {
                    "seller_orders" => await HandleSellerOrdersQueryAsync(seller, normalizedMessage),
                    "seller_sales" => await HandleSellerSalesQueryAsync(seller, normalizedMessage),
                    "seller_payment" => await HandleSellerPaymentQueryAsync(seller),
                    "seller_products" => await HandleSellerProductsQueryAsync(seller, normalizedMessage),
                    "seller_improve" => await HandleSellerImprovementQueryAsync(seller),
                    "seller_messages" => await HandleSellerMessagesQueryAsync(seller),
                    "seller_dashboard" => await HandleSellerDashboardQueryAsync(seller),
                    _ => await HandleGeneralSellerQueryAsync(seller, normalizedMessage)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing seller query for {SellerId}", sellerId);
                return new AIChatResponse
                {
                    Message = "দুঃখিত, আপনার অনুরোধ প্রক্রিয়া করতে সমস্যা হয়েছে। অনুগ্রহ করে আবার চেষ্টা করুন।",
                    IsSuccessful = false
                };
            }
        }

        private string DetectSellerIntent(string message)
        {
            foreach (var (intent, patterns) in _sellerIntentPatterns)
            {
                if (patterns.Any(p => message.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    return intent;
                }
            }
            return "general";
        }

        private async Task<AIChatResponse> HandleSellerOrdersQueryAsync(Seller seller, string message)
        {
            var summary = await GetSellerOrderSummaryAsync(seller.UserId!);

            var response = $"📦 **আপনার অর্ডার সামারি**\n\n" +
                          $"🆕 আজকের অর্ডার: **{summary.TodayOrders}টি**\n" +
                          $"⏳ পেন্ডিং: **{summary.PendingOrders}টি**\n" +
                          $"🔄 প্রসেসিং: **{summary.ProcessingOrders}টি**\n" +
                          $"🚚 শিপড: **{summary.ShippedOrders}টি**\n" +
                          $"✅ ডেলিভারড: **{summary.DeliveredOrders}টি**\n";

            if (summary.ReturnRequests > 0)
            {
                response += $"↩️ রিটার্ন রিকোয়েস্ট: **{summary.ReturnRequests}টি**\n";
            }

            response += $"\n💰 আজকের রেভিনিউ: **৳{summary.TodayRevenue:N0}**";

            if (summary.PendingOrders > 0)
            {
                response += "\n\n⚠️ পেন্ডিং অর্ডার গুলো দ্রুত প্রসেস করুন!";
            }

            return new AIChatResponse
            {
                Message = response,
                DetectedIntent = "seller_orders",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "পেন্ডিং দেখুন", Action = "send_message", Payload = "pending orders দেখাও" },
                    new() { Text = "আজকের অর্ডার", Action = "send_message", Payload = "আজকের orders" },
                    new() { Text = "Order Dashboard", Action = "open_url", Payload = "/Seller/Order" }
                }
            };
        }

        private async Task<AIChatResponse> HandleSellerSalesQueryAsync(Seller seller, string message)
        {
            var period = "today";
            if (message.Contains("week") || message.Contains("সপ্তাহ")) period = "week";
            else if (message.Contains("month") || message.Contains("মাস")) period = "month";

            var analytics = await GetSellerSalesAnalyticsAsync(seller.UserId!, period);

            var periodText = period switch
            {
                "week" => "এই সপ্তাহের",
                "month" => "এই মাসের",
                _ => "আজকের"
            };

            var growthEmoji = analytics.IsGrowth ? "📈" : "📉";
            var growthText = analytics.IsGrowth ? "বৃদ্ধি" : "হ্রাস";

            var response = $"💰 **{periodText} সেলস রিপোর্ট**\n\n" +
                          $"🛍️ মোট বিক্রি: **{analytics.TotalItemsSold}টি**\n" +
                          $"📦 মোট অর্ডার: **{analytics.TotalOrders}টি**\n" +
                          $"💵 মোট রেভিনিউ: **৳{analytics.TotalRevenue:N0}**\n" +
                          $"📊 গড় অর্ডার ভ্যালু: **৳{analytics.AverageOrderValue:N0}**\n" +
                          $"{growthEmoji} আগের {periodText} থেকে **{Math.Abs(analytics.ComparisonPercentage):N1}%** {growthText}";

            if (analytics.TopProducts?.Any() == true)
            {
                response += "\n\n🏆 **টপ সেলিং প্রোডাক্ট:**";
                foreach (var product in analytics.TopProducts.Take(3))
                {
                    response += $"\n• {product.ProductName} ({product.QuantitySold}টি)";
                }
            }

            return new AIChatResponse
            {
                Message = response,
                DetectedIntent = "seller_sales",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "সাপ্তাহিক রিপোর্ট", Action = "send_message", Payload = "এই সপ্তাহের sales" },
                    new() { Text = "মাসিক রিপোর্ট", Action = "send_message", Payload = "এই মাসের sales" },
                    new() { Text = "Reports Dashboard", Action = "open_url", Payload = "/Seller/Reports" }
                }
            };
        }

        private async Task<AIChatResponse> HandleSellerPaymentQueryAsync(Seller seller)
        {
            var payment = await GetSellerPaymentStatusAsync(seller.UserId!);

            var response = $"💳 **পেমেন্ট স্ট্যাটাস**\n\n" +
                          $"💰 মোট আয়: **৳{payment.TotalEarnings:N0}**\n" +
                          $"⏳ পেন্ডিং: **৳{payment.PendingAmount:N0}**\n" +
                          $"✅ উইথড্রযোগ্য: **৳{payment.AvailableForWithdraw:N0}**\n" +
                          $"💸 উইথড্র করা হয়েছে: **৳{payment.WithdrawnAmount:N0}**\n";

            if (payment.HoldAmount > 0)
            {
                response += $"🔒 হোল্ড: **৳{payment.HoldAmount:N0}**\n";
            }

            if (payment.NextPayoutDate.HasValue)
            {
                response += $"\n📅 পরবর্তী পেআউট: **{payment.NextPayoutDate:dd MMM yyyy}**";
            }

            if (payment.AvailableForWithdraw > 0)
            {
                response += "\n\n💡 আপনি এখনই উইথড্র রিকোয়েস্ট করতে পারেন!";
            }

            return new AIChatResponse
            {
                Message = response,
                DetectedIntent = "seller_payment",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "উইথড্র করুন", Action = "open_url", Payload = "/Seller/Transaction" },
                    new() { Text = "পেমেন্ট হিস্ট্রি", Action = "open_url", Payload = "/Seller/Transaction/History" },
                    new() { Text = "পেন্ডিং পেমেন্ট", Action = "send_message", Payload = "pending payment details" }
                }
            };
        }

        private async Task<AIChatResponse> HandleSellerProductsQueryAsync(Seller seller, string message)
        {
            var insights = await GetSellerProductInsightsAsync(seller.UserId!);

            var response = $"📊 **প্রোডাক্ট ইনসাইটস**\n\n" +
                          $"📦 মোট প্রোডাক্ট: **{insights.TotalProducts}টি**\n" +
                          $"✅ অ্যাক্টিভ: **{insights.ActiveProducts}টি**\n" +
                          $"❌ আউট অফ স্টক: **{insights.OutOfStockProducts}টি**\n" +
                          $"⚠️ লো স্টক: **{insights.LowStockProducts}টি**\n" +
                          $"📝 ড্রাফট: **{insights.DraftProducts}টি**\n";

            if (insights.OutOfStockProducts > 0 || insights.LowStockProducts > 0)
            {
                response += "\n\n🚨 **অ্যাটেনশন নিডেড:**";

                if (insights.LowStockItems?.Any() == true)
                {
                    response += "\n\n📉 **লো স্টক প্রোডাক্ট:**";
                    foreach (var item in insights.LowStockItems.Take(3))
                    {
                        var status = item.IsOutOfStock ? "❌ শেষ" : $"⚠️ {item.CurrentStock}টি";
                        response += $"\n• {item.ProductName} - {status}";
                    }
                }
            }

            if (insights.BestSellers?.Any() == true)
            {
                response += "\n\n🏆 **বেস্ট সেলার:**";
                foreach (var item in insights.BestSellers.Take(3))
                {
                    response += $"\n• {item.ProductName} ({item.QuantitySold}টি বিক্রি)";
                }
            }

            return new AIChatResponse
            {
                Message = response,
                DetectedIntent = "seller_products",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "স্টক আপডেট", Action = "open_url", Payload = "/Seller/Product" },
                    new() { Text = "নতুন প্রোডাক্ট", Action = "open_url", Payload = "/Seller/Product/Create" },
                    new() { Text = "লো স্টক লিস্ট", Action = "send_message", Payload = "low stock products দেখাও" }
                }
            };
        }

        private async Task<AIChatResponse> HandleSellerImprovementQueryAsync(Seller seller)
        {
            var tips = await GetSellerImprovementTipsAsync(seller.UserId!);

            var ratingStars = tips.ShopRating >= 4.5m ? "⭐⭐⭐⭐⭐" :
                             tips.ShopRating >= 4.0m ? "⭐⭐⭐⭐" :
                             tips.ShopRating >= 3.0m ? "⭐⭐⭐" :
                             tips.ShopRating >= 2.0m ? "⭐⭐" : "⭐";

            var response = $"⭐ **শপ পারফরম্যান্স**\n\n" +
                          $"রেটিং: {ratingStars} **{tips.ShopRating:N1}/5** ({tips.TotalReviews} রিভিউ)\n\n" +
                          $"📊 **মেট্রিক্স:**\n" +
                          $"• রেসপন্স রেট: **{tips.ResponseRate:N0}%**\n" +
                          $"• শিপ অন টাইম: **{tips.ShipOnTimeRate:N0}%**\n" +
                          $"• ক্যান্সেলেশন রেট: **{tips.CancellationRate:N1}%**\n" +
                          $"• রিটার্ন রেট: **{tips.ReturnRate:N1}%**\n";

            if (tips.Tips?.Any() == true)
            {
                response += "\n💡 **ইমপ্রুভমেন্ট টিপস:**";
                foreach (var tip in tips.Tips.Take(3))
                {
                    var priority = tip.Priority == "high" ? "🔴" : tip.Priority == "medium" ? "🟡" : "🟢";
                    response += $"\n{priority} {tip.Title}";
                }
            }

            return new AIChatResponse
            {
                Message = response,
                DetectedIntent = "seller_improve",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "রিভিউ দেখুন", Action = "open_url", Payload = "/Seller/Shop/Reviews" },
                    new() { Text = "শপ সেটিংস", Action = "open_url", Payload = "/Seller/Shop" },
                    new() { Text = "বিস্তারিত টিপস", Action = "send_message", Payload = "সব improvement tips দেখাও" }
                }
            };
        }

        private async Task<AIChatResponse> HandleSellerMessagesQueryAsync(Seller seller)
        {
            var messages = await GetSellerMessageSummaryAsync(seller.UserId!);

            var response = $"💬 **মেসেজ সামারি**\n\n" +
                          $"📩 আনরেড মেসেজ: **{messages.UnreadMessages}টি**\n" +
                          $"💬 মোট কনভার্সেশন: **{messages.TotalConversations}টি**\n" +
                          $"❓ পেন্ডিং জিজ্ঞাসা: **{messages.PendingInquiries}টি**\n" +
                          $"🎫 ওপেন টিকেট: **{messages.OpenTickets}টি**\n";

            if (messages.UrgentTickets > 0)
            {
                response += $"🚨 আর্জেন্ট টিকেট: **{messages.UrgentTickets}টি**\n";
            }

            response += $"\n⏱️ গড় রেসপন্স টাইম: **{messages.AverageResponseTime:N0} মিনিট**";

            if (messages.UnreadMessages > 0)
            {
                response += "\n\n⚠️ আনরেড মেসেজ দ্রুত রেসপন্ড করুন - এটি আপনার রেটিং এ প্রভাব ফেলে!";
            }

            return new AIChatResponse
            {
                Message = response,
                DetectedIntent = "seller_messages",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "মেসেজ দেখুন", Action = "open_url", Payload = "/Seller/Messages" },
                    new() { Text = "টিকেট দেখুন", Action = "open_url", Payload = "/Seller/Messages/Tickets" },
                    new() { Text = "Q&A দেখুন", Action = "open_url", Payload = "/Seller/QA" }
                }
            };
        }

        private async Task<AIChatResponse> HandleSellerDashboardQueryAsync(Seller seller)
        {
            var orderSummary = await GetSellerOrderSummaryAsync(seller.UserId!);
            var paymentStatus = await GetSellerPaymentStatusAsync(seller.UserId!);
            var productInsights = await GetSellerProductInsightsAsync(seller.UserId!);

            var response = $"📊 **{seller.ShopName} - ড্যাশবোর্ড**\n\n" +
                          $"📦 **অর্ডার:**\n" +
                          $"• আজকের: {orderSummary.TodayOrders}টি | পেন্ডিং: {orderSummary.PendingOrders}টি\n" +
                          $"• আজকের রেভিনিউ: ৳{orderSummary.TodayRevenue:N0}\n\n" +
                          $"💰 **পেমেন্ট:**\n" +
                          $"• পেন্ডিং: ৳{paymentStatus.PendingAmount:N0}\n" +
                          $"• উইথড্রযোগ্য: ৳{paymentStatus.AvailableForWithdraw:N0}\n\n" +
                          $"📦 **প্রোডাক্ট:**\n" +
                          $"• অ্যাক্টিভ: {productInsights.ActiveProducts}টি\n" +
                          $"• আউট অফ স্টক: {productInsights.OutOfStockProducts}টি\n" +
                          $"• লো স্টক: {productInsights.LowStockProducts}টি";

            // Add alerts if needed
            var alerts = new List<string>();
            if (orderSummary.PendingOrders > 5) alerts.Add("⚠️ পেন্ডিং অর্ডার বেশি");
            if (productInsights.OutOfStockProducts > 0) alerts.Add("❌ কিছু প্রোডাক্ট স্টক আউট");
            if (paymentStatus.AvailableForWithdraw > 5000) alerts.Add("💰 উইথড্র করার টাকা আছে");

            if (alerts.Any())
            {
                response += "\n\n🔔 **অ্যালার্ট:**";
                foreach (var alert in alerts)
                {
                    response += $"\n{alert}";
                }
            }

            return new AIChatResponse
            {
                Message = response,
                DetectedIntent = "seller_dashboard",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "অর্ডার ম্যানেজ", Action = "open_url", Payload = "/Seller/Order" },
                    new() { Text = "পেমেন্ট দেখুন", Action = "open_url", Payload = "/Seller/Transaction" },
                    new() { Text = "প্রোডাক্ট দেখুন", Action = "open_url", Payload = "/Seller/Product" },
                    new() { Text = "Dashboard", Action = "open_url", Payload = "/Seller/Dashboard" }
                }
            };
        }

        private async Task<AIChatResponse> HandleGeneralSellerQueryAsync(Seller seller, string message)
        {
            // Default helpful response with navigation options
            return new AIChatResponse
            {
                Message = $"আমি আপনার সেলার সহকারী! 🏪\n\n" +
                         "আপনি আমাকে জিজ্ঞাসা করতে পারেন:\n\n" +
                         "📦 **অর্ডার সংক্রান্ত:**\n" +
                         "• \"আজকের অর্ডার কত?\"\n" +
                         "• \"পেন্ডিং অর্ডার দেখাও\"\n\n" +
                         "💰 **সেলস ও পেমেন্ট:**\n" +
                         "• \"আজকের সেলস কত?\"\n" +
                         "• \"পেমেন্ট স্ট্যাটাস দেখাও\"\n\n" +
                         "📊 **প্রোডাক্ট:**\n" +
                         "• \"লো স্টক প্রোডাক্ট\"\n" +
                         "• \"বেস্ট সেলার কি?\"\n\n" +
                         "⭐ **শপ ইমপ্রুভমেন্ট:**\n" +
                         "• \"শপ রেটিং কত?\"\n" +
                         "• \"ইমপ্রুভমেন্ট টিপস দাও\"\n\n" +
                         "কি জানতে চান?",
                DetectedIntent = "general_help",
                QuickReplies = new List<QuickReplyButton>
                {
                    new() { Text = "📦 অর্ডার সামারি", Action = "send_message", Payload = "order summary" },
                    new() { Text = "💰 সেলস রিপোর্ট", Action = "send_message", Payload = "sales report" },
                    new() { Text = "💳 পেমেন্ট", Action = "send_message", Payload = "payment status" },
                    new() { Text = "📊 ড্যাশবোর্ড", Action = "send_message", Payload = "dashboard summary" }
                }
            };
        }

        // ========== Seller Data Methods ==========

        public async Task<SellerOrderSummaryResponse> GetSellerOrderSummaryAsync(string sellerId)
        {
            try
            {
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
                if (seller == null)
                {
                    return new SellerOrderSummaryResponse { Success = false, Message = "Seller not found" };
                }

                var todayStart = DateTime.Today;

                var orderStats = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id)
                    .GroupBy(oi => oi.Order!.Status)
                    .Select(g => new { Status = g.Key, Count = g.Select(x => x.OrderId).Distinct().Count() })
                    .ToListAsync();

                var todayOrders = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id && oi.Order!.OrderDate >= todayStart)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();

                var todayRevenue = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id && oi.Order!.OrderDate >= todayStart &&
                           oi.Order.Status != OrderStatus.Cancelled && oi.Order.Status != OrderStatus.Returned)
                    .SumAsync(oi => oi.UnitPrice * oi.Quantity);

                var returnRequests = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id && oi.Order!.Status == OrderStatus.Returned)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();

                return new SellerOrderSummaryResponse
                {
                    Success = true,
                    TodayOrders = todayOrders,
                    PendingOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Pending)?.Count ?? 0,
                    ProcessingOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Processing)?.Count ?? 0,
                    ShippedOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Shipped)?.Count ?? 0,
                    DeliveredOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Delivered)?.Count ?? 0,
                    CancelledOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Cancelled)?.Count ?? 0,
                    ReturnRequests = returnRequests,
                    TodayRevenue = todayRevenue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller order summary");
                return new SellerOrderSummaryResponse { Success = false, Message = "Error fetching data" };
            }
        }

        public async Task<SellerOrdersResponse> GetSellerOrdersAsync(string sellerId, string? status = null, int count = 10)
        {
            try
            {
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
                if (seller == null)
                {
                    return new SellerOrdersResponse { Success = false, Message = "Seller not found" };
                }

                var query = _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id)
                    .Include(oi => oi.Order)
                    .ThenInclude(o => o!.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    query = query.Where(oi => oi.Order!.Status == orderStatus);
                }

                var orders = await query
                    .GroupBy(oi => oi.Order)
                    .Select(g => new SellerOrderInfo
                    {
                        Id = g.Key!.Id,
                        OrderNumber = g.Key.OrderNumber,
                        OrderDate = g.Key.OrderDate,
                        CustomerName = g.Key.User != null ? g.Key.User.FullName : "Guest",
                        Status = g.Key.Status.ToString(),
                        StatusBangla = GetOrderStatusBangla(g.Key.Status),
                        Amount = g.Sum(x => x.UnitPrice * x.Quantity),
                        ItemCount = g.Sum(x => x.Quantity),
                        PaymentStatus = g.Key.PaymentStatus.ToString(),
                        NeedsAction = g.Key.Status == OrderStatus.Pending || g.Key.Status == OrderStatus.Returned,
                        ActionRequired = g.Key.Status == OrderStatus.Pending ? "প্রসেস করুন" :
                                        g.Key.Status == OrderStatus.Returned ? "রিটার্ন হ্যান্ডেল করুন" : null,
                        OrderUrl = $"/Seller/Order/Details/{g.Key.Id}"
                    })
                    .OrderByDescending(o => o.OrderDate)
                    .Take(count)
                    .ToListAsync();

                return new SellerOrdersResponse
                {
                    Success = true,
                    Orders = orders,
                    TotalCount = orders.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller orders");
                return new SellerOrdersResponse { Success = false, Message = "Error fetching orders" };
            }
        }

        private static string GetOrderStatusBangla(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "পেন্ডিং",
                OrderStatus.Confirmed => "কনফার্মড",
                OrderStatus.Processing => "প্রসেসিং",
                OrderStatus.Shipped => "শিপড",
                OrderStatus.Delivered => "ডেলিভারড",
                OrderStatus.Cancelled => "ক্যান্সেলড",
                OrderStatus.Returned => "রিটার্নড",
                OrderStatus.Refunded => "রিফান্ডেড",
                _ => status.ToString()
            };
        }

        public async Task<SellerSalesAnalyticsResponse> GetSellerSalesAnalyticsAsync(string sellerId, string period = "today")
        {
            try
            {
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
                if (seller == null)
                {
                    return new SellerSalesAnalyticsResponse { Success = false, Message = "Seller not found" };
                }

                var (startDate, endDate, prevStartDate, prevEndDate) = GetDateRangeForPeriod(period);

                // Current period stats
                var currentStats = await GetSalesStatsForPeriod(seller.Id, startDate, endDate);
                var previousStats = await GetSalesStatsForPeriod(seller.Id, prevStartDate, prevEndDate);

                // Calculate comparison
                var comparison = previousStats.TotalRevenue > 0
                    ? ((currentStats.TotalRevenue - previousStats.TotalRevenue) / previousStats.TotalRevenue) * 100
                    : (currentStats.TotalRevenue > 0 ? 100 : 0);

                // Get daily breakdown for week/month
                List<SellerDailySales>? dailyBreakdown = null;
                if (period != "today")
                {
                    dailyBreakdown = await _db.OrderItems
                        .Where(oi => oi.SellerId == seller.Id &&
                                    oi.Order!.OrderDate >= startDate &&
                                    oi.Order.OrderDate <= endDate &&
                                    oi.Order.Status != OrderStatus.Cancelled)
                        .GroupBy(oi => oi.Order!.OrderDate.Date)
                        .Select(g => new SellerDailySales
                        {
                            Date = g.Key,
                            Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                            Orders = g.Select(x => x.OrderId).Distinct().Count()
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                }

                // Get top products
                var topProducts = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id &&
                                oi.Order!.OrderDate >= startDate &&
                                oi.Order.OrderDate <= endDate &&
                                oi.Order.Status != OrderStatus.Cancelled)
                    .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.ImageUrl })
                    .Select(g => new SellerTopProduct
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        QuantitySold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                        ImageUrl = g.Key.ImageUrl
                    })
                    .OrderByDescending(x => x.QuantitySold)
                    .Take(5)
                    .ToListAsync();

                return new SellerSalesAnalyticsResponse
                {
                    Success = true,
                    Period = period,
                    TotalSales = currentStats.TotalItems,
                    TotalRevenue = currentStats.TotalRevenue,
                    TotalOrders = currentStats.TotalOrders,
                    TotalItemsSold = currentStats.TotalItems,
                    AverageOrderValue = currentStats.TotalOrders > 0 ? currentStats.TotalRevenue / currentStats.TotalOrders : 0,
                    ComparisonPercentage = comparison,
                    IsGrowth = comparison >= 0,
                    DailyBreakdown = dailyBreakdown,
                    TopProducts = topProducts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller sales analytics");
                return new SellerSalesAnalyticsResponse { Success = false, Message = "Error fetching analytics" };
            }
        }

        private (DateTime startDate, DateTime endDate, DateTime prevStartDate, DateTime prevEndDate) GetDateRangeForPeriod(string period)
        {
            var today = DateTime.Today;
            return period switch
            {
                "week" => (today.AddDays(-7), today, today.AddDays(-14), today.AddDays(-7)),
                "month" => (today.AddDays(-30), today, today.AddDays(-60), today.AddDays(-30)),
                _ => (today, today.AddDays(1), today.AddDays(-1), today)
            };
        }

        private async Task<(decimal TotalRevenue, int TotalOrders, int TotalItems)> GetSalesStatsForPeriod(int sellerId, DateTime startDate, DateTime endDate)
        {
            var stats = await _db.OrderItems
                .Where(oi => oi.SellerId == sellerId &&
                            oi.Order!.OrderDate >= startDate &&
                            oi.Order.OrderDate < endDate &&
                            oi.Order.Status != OrderStatus.Cancelled &&
                            oi.Order.Status != OrderStatus.Returned)
                .GroupBy(oi => 1)
                .Select(g => new
                {
                    TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity),
                    TotalOrders = g.Select(x => x.OrderId).Distinct().Count(),
                    TotalItems = g.Sum(x => x.Quantity)
                })
                .FirstOrDefaultAsync();

            return (stats?.TotalRevenue ?? 0, stats?.TotalOrders ?? 0, stats?.TotalItems ?? 0);
        }

        public async Task<SellerPaymentStatusResponse> GetSellerPaymentStatusAsync(string sellerId)
        {
            try
            {
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
                if (seller == null)
                {
                    return new SellerPaymentStatusResponse { Success = false, Message = "Seller not found" };
                }

                // Get payment statistics from SellerPayments table
                var totalEarnings = await _db.SellerPayments
                    .Where(sp => sp.SellerId == seller.Id)
                    .SumAsync(sp => sp.Amount);

                var withdrawnAmount = await _db.SellerPayments
                    .Where(sp => sp.SellerId == seller.Id && sp.Status == SellerPaymentStatus.Paid)
                    .SumAsync(sp => sp.Amount);

                var pendingAmount = await _db.SellerPayments
                    .Where(sp => sp.SellerId == seller.Id && sp.Status == SellerPaymentStatus.Pending)
                    .SumAsync(sp => sp.Amount);

                var holdAmount = await _db.SellerPayments
                    .Where(sp => sp.SellerId == seller.Id && sp.Status == SellerPaymentStatus.UnderReview)
                    .SumAsync(sp => sp.Amount);

                // Calculate available amount (from delivered orders not yet paid)
                var availableAmount = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id &&
                                oi.Order!.Status == OrderStatus.Delivered &&
                                oi.Order.PaymentStatus == PaymentStatus.Completed)
                    .SumAsync(oi => oi.UnitPrice * oi.Quantity * (1 - (seller.CommissionRate / 100)));

                // Recent payments
                var recentPayments = await _db.SellerPayments
                    .Where(sp => sp.SellerId == seller.Id && sp.Status == SellerPaymentStatus.Paid)
                    .OrderByDescending(sp => sp.CompletedAt ?? sp.ProcessedAt ?? sp.CreatedAt)
                    .Take(5)
                    .Select(sp => new SellerPaymentInfo
                    {
                        Id = sp.Id,
                        Amount = sp.Amount,
                        Status = sp.Status.ToString(),
                        Date = sp.CompletedAt ?? sp.ProcessedAt ?? sp.CreatedAt,
                        TransactionId = sp.TransactionReference,
                        PaymentMethod = sp.PaymentMethod.ToString()
                    })
                    .ToListAsync();

                return new SellerPaymentStatusResponse
                {
                    Success = true,
                    TotalEarnings = totalEarnings,
                    PendingAmount = pendingAmount,
                    AvailableForWithdraw = availableAmount - withdrawnAmount - holdAmount,
                    WithdrawnAmount = withdrawnAmount,
                    HoldAmount = holdAmount,
                    NextPayoutDate = DateTime.Today.AddDays(1).Day > 15 ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1) : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 15),
                    RecentPayments = recentPayments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller payment status");
                return new SellerPaymentStatusResponse { Success = false, Message = "Error fetching payment data" };
            }
        }

        public async Task<SellerProductInsightsResponse> GetSellerProductInsightsAsync(string sellerId)
        {
            try
            {
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
                if (seller == null)
                {
                    return new SellerProductInsightsResponse { Success = false, Message = "Seller not found" };
                }

                var products = await _db.Products
                    .Where(p => p.SellerId == seller.Id)
                    .ToListAsync();

                var totalProducts = products.Count;
                var activeProducts = products.Count(p => p.IsAvailable && p.Status == ProductStatus.Active);
                var outOfStockProducts = products.Count(p => p.Stock == 0 && p.IsAvailable);
                var lowStockProducts = products.Count(p => p.Stock > 0 && p.Stock <= 5 && p.IsAvailable);
                var draftProducts = products.Count(p => p.Status == ProductStatus.Draft || p.Status == ProductStatus.Inactive);

                // Low stock items
                var lowStockItems = products
                    .Where(p => p.Stock <= 5 && p.IsAvailable)
                    .OrderBy(p => p.Stock)
                    .Take(5)
                    .Select(p => new SellerLowStockProduct
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        CurrentStock = p.Stock,
                        MinimumStock = 5,
                        ImageUrl = p.ImageUrl,
                        ProductUrl = $"/Seller/Product/Edit/{p.Id}",
                        IsOutOfStock = p.Stock == 0
                    })
                    .ToList();

                // Best sellers (last 30 days)
                var thirtyDaysAgo = DateTime.Today.AddDays(-30);
                var bestSellers = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id &&
                                oi.Order!.OrderDate >= thirtyDaysAgo &&
                                oi.Order.Status != OrderStatus.Cancelled)
                    .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.ImageUrl })
                    .Select(g => new SellerTopProduct
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        QuantitySold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                        ImageUrl = g.Key.ImageUrl
                    })
                    .OrderByDescending(x => x.QuantitySold)
                    .Take(5)
                    .ToListAsync();

                return new SellerProductInsightsResponse
                {
                    Success = true,
                    TotalProducts = totalProducts,
                    ActiveProducts = activeProducts,
                    OutOfStockProducts = outOfStockProducts,
                    LowStockProducts = lowStockProducts,
                    DraftProducts = draftProducts,
                    LowStockItems = lowStockItems,
                    BestSellers = bestSellers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller product insights");
                return new SellerProductInsightsResponse { Success = false, Message = "Error fetching insights" };
            }
        }

        public async Task<SellerImprovementTipsResponse> GetSellerImprovementTipsAsync(string sellerId)
        {
            try
            {
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
                if (seller == null)
                {
                    return new SellerImprovementTipsResponse { Success = false, Message = "Seller not found" };
                }

                // Calculate metrics
                var thirtyDaysAgo = DateTime.Today.AddDays(-30);

                var totalOrders = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id && oi.Order!.OrderDate >= thirtyDaysAgo)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();

                var deliveredOnTime = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id &&
                                oi.Order!.OrderDate >= thirtyDaysAgo &&
                                oi.Order.Status == OrderStatus.Delivered)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();

                var cancelledOrders = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id &&
                                oi.Order!.OrderDate >= thirtyDaysAgo &&
                                oi.Order.Status == OrderStatus.Cancelled)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();

                var returnedOrders = await _db.OrderItems
                    .Where(oi => oi.SellerId == seller.Id &&
                                oi.Order!.OrderDate >= thirtyDaysAgo &&
                                (oi.Order.Status == OrderStatus.Returned || oi.Order.Status == OrderStatus.Refunded))
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();

                var shipOnTimeRate = totalOrders > 0 ? (decimal)deliveredOnTime / totalOrders * 100 : 100;
                var cancellationRate = totalOrders > 0 ? (decimal)cancelledOrders / totalOrders * 100 : 0;
                var returnRate = totalOrders > 0 ? (decimal)returnedOrders / totalOrders * 100 : 0;

                // Generate tips based on metrics
                var tips = new List<SellerImprovementTip>();

                if (seller.Rating < 4.0m)
                {
                    tips.Add(new SellerImprovementTip
                    {
                        Title = "শপ রেটিং বাড়ান",
                        Description = "কাস্টমার সার্ভিস উন্নত করুন এবং প্রোডাক্ট কোয়ালিটি নিশ্চিত করুন",
                        Priority = "high",
                        Category = "rating",
                        Icon = "fas fa-star"
                    });
                }

                if (cancellationRate > 5)
                {
                    tips.Add(new SellerImprovementTip
                    {
                        Title = "ক্যান্সেলেশন রেট কমান",
                        Description = "স্টক আপডেট রাখুন এবং সঠিক প্রোডাক্ট বিবরণ দিন",
                        Priority = "high",
                        Category = "orders",
                        Icon = "fas fa-times-circle"
                    });
                }

                if (returnRate > 3)
                {
                    tips.Add(new SellerImprovementTip
                    {
                        Title = "রিটার্ন রেট কমান",
                        Description = "প্রোডাক্ট ছবি ও বিবরণ সঠিক রাখুন, প্যাকেজিং উন্নত করুন",
                        Priority = "medium",
                        Category = "returns",
                        Icon = "fas fa-undo"
                    });
                }

                if (shipOnTimeRate < 90)
                {
                    tips.Add(new SellerImprovementTip
                    {
                        Title = "শিপিং টাইম উন্নত করুন",
                        Description = "অর্ডার দ্রুত প্রসেস করুন এবং শিপমেন্ট ট্র্যাক করুন",
                        Priority = "medium",
                        Category = "shipping",
                        Icon = "fas fa-shipping-fast"
                    });
                }

                // Add general tips if no issues
                if (!tips.Any())
                {
                    tips.Add(new SellerImprovementTip
                    {
                        Title = "নতুন প্রোডাক্ট যোগ করুন",
                        Description = "ট্রেন্ডিং প্রোডাক্ট যোগ করে সেলস বাড়ান",
                        Priority = "low",
                        Category = "growth",
                        Icon = "fas fa-plus-circle"
                    });
                }

                // Get review count from products
                var reviewCount = await _db.ProductReviews
                    .Where(r => r.Product != null && r.Product.SellerId == seller.Id)
                    .CountAsync();

                return new SellerImprovementTipsResponse
                {
                    Success = true,
                    ShopRating = seller.Rating,
                    TotalReviews = reviewCount,
                    ResponseRate = 95, // TODO: Calculate actual response rate
                    ShipOnTimeRate = shipOnTimeRate,
                    CancellationRate = cancellationRate,
                    ReturnRate = returnRate,
                    Tips = tips
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller improvement tips");
                return new SellerImprovementTipsResponse { Success = false, Message = "Error fetching tips" };
            }
        }

        public async Task<SellerMessageSummaryResponse> GetSellerMessageSummaryAsync(string sellerId)
        {
            try
            {
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
                if (seller == null)
                {
                    return new SellerMessageSummaryResponse { Success = false, Message = "Seller not found" };
                }

                // Get message statistics
                var conversations = await _db.SellerConversations
                    .Where(c => c.SellerId == seller.Id)
                    .ToListAsync();

                var unreadMessages = await _db.SellerMessages
                    .Where(m => m.Conversation!.SellerId == seller.Id && !m.IsRead && !m.IsSentBySeller)
                    .CountAsync();

                var recentMessages = await _db.SellerMessages
                    .Where(m => m.Conversation!.SellerId == seller.Id && !m.IsSentBySeller)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .Select(m => new SellerRecentMessage
                    {
                        ConversationId = m.ConversationId,
                        CustomerName = m.Conversation!.Buyer != null ? m.Conversation.Buyer.FullName : "Customer",
                        LastMessage = m.Message.Length > 50 ? m.Message.Substring(0, 50) + "..." : m.Message,
                        MessageTime = m.CreatedAt,
                        IsUnread = !m.IsRead,
                        RelatedOrderNumber = m.Conversation.Order != null ? m.Conversation.Order.OrderNumber : null,
                        ConversationUrl = $"/Seller/Messages/Conversation/{m.ConversationId}"
                    })
                    .ToListAsync();

                return new SellerMessageSummaryResponse
                {
                    Success = true,
                    UnreadMessages = unreadMessages,
                    TotalConversations = conversations.Count,
                    PendingInquiries = conversations.Count(c => !c.IsClosedBySeller && !c.IsClosedByBuyer),
                    OpenTickets = 0, // TODO: Implement ticket system
                    UrgentTickets = 0,
                    AverageResponseTime = 30, // TODO: Calculate actual response time
                    RecentMessages = recentMessages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller message summary");
                return new SellerMessageSummaryResponse { Success = false, Message = "Error fetching messages" };
            }
        }

        #endregion
    }
}
