
    /// <summary>
    /// Класс GenesisLogElement - элемент лога событий Генезиса (для записи в БД Mongo)
    /// </summary>
    public class GenesisLogElement
    {
        //дата возникновения события
        private DateTime _date;

        /// <summary>
        /// Конструктор, инициализирует новое событие
        /// </summary>
        /// <param name="message">Текст сообщения в событии</param>
        public GenesisLogElement(string message)
        {
            Login = Environment.UserName.ToLower();
            _date = DateTime.Now;
            Wks = Environment.MachineName;
            Message = message;
        }

        /// <summary>
        /// Свойство - GUID события
        /// </summary>
        [BsonId(IdGenerator = typeof (GuidGenerator))]
        public Guid Id { get; set; }

        /// <summary>
        /// Свойство - логин пользователя, инициировавшего событие
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Свойство - дата возникновения события
        /// </summary>
        [BsonDateTimeOptions]
        public DateTime Date
        {
            get { return _date.ToLocalTime(); }
            set { _date = value.ToUniversalTime(); }
        }

        /// <summary>
        /// Свойство - сообщение в событии
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Свойство - имя NetBIOS компьютера, на котором произошло событие
        /// </summary>
        public string Wks { get; set; }
    }

    /// <summary>
    /// Класс MongoDBTraceListener - регистратор отладночной информации в коллекции Mongo
    /// </summary>
    internal class MongoDBTraceListener : TraceListener
    {
        //название коллекции с отладочной информацией ("GenesisLog")
        private readonly string _collectionName;

        //строка соединения с сервером Mongo ("mongodb://ssmrdb2:27017")
        private readonly string _connectionString;

        //название БД Монго, в которой хранится отладночая информация ("Logs")
        private readonly string _databaseName;

        //границы часового диапазона, в котором можно производить запись сообщений
        private readonly int endHourIncluded = 21;
        private readonly int startHourIncluded = 7;
        
        /// <summary>
        /// Конструктор, принимает на вход названия объектов Mongo, нужных для записи событий
        /// </summary>
        /// <param name="connectionString">Строка соединения с сервером Mongo</param>
        /// <param name="databaseName">Название базы данных Mongo</param>
        /// <param name="collectionName">Название коллекции с отладочной информацией</param>
        public MongoDBTraceListener(string connectionString, string databaseName, string collectionName)
        {
            _connectionString = connectionString;
            _databaseName = databaseName;
            _collectionName = collectionName;
        }

        /// <summary>
        /// Запись сообщения в БД Mongo
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        private void WriteMessage(string message)
        {
            int hour = DateTime.Now.Hour;
            if (hour >= startHourIncluded && hour <= endHourIncluded)
            {
                MongoServer server = new MongoClient(_connectionString).GetServer();// MongoServer.Create(_connectionString);
                var db = server.GetDatabase(_databaseName);
                if (!db.CollectionExists(_collectionName))
                {
                    CollectionOptionsBuilder b=new CollectionOptionsBuilder();
                    b = b.SetCapped(true).SetMaxSize(MB(500));
                    db.CreateCollection(_collectionName,b);
                }
                var collection = db.GetCollection<GenesisLogElement>(_collectionName);
                collection.Insert(new GenesisLogElement(message));
            }
        }

        private long MB(int size)
        {
            return (long) (Math.Pow(1024, 2)*size);
        }

        /// <summary>
        /// Попытка записи сообщения в БД Mongo
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <returns>Успешность попытки</returns>
        private bool TryWriteMessage(string message)
        {
            try
            {
                WriteMessage(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Запись сообщения в БД Mongo
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public override void Write(string message)
        {
            WriteLine(message);
        }

        /// <summary>
        /// Безопасная запись сообщения в БД Mongo (не вызывает исключений)
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public override void WriteLine(string message)
        {
            TryWriteMessage(message);
        }
    }
