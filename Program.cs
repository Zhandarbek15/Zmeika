using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

class Program
{
    /// <summary>
    /// Мэйн \(^-^)/
    /// </summary>
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.SetWindowPosition(0, 0);
        Console.WriteLine("\t\t КОНСОЛЬДЫ БҮКІЛ ЭКРАНҒА ҚОЙСАҢЫЗ ЫҢҒАЙЛЫ БОЛАДЫ!!!\n\n");
        Console.Write("Алаң биіктігін ендігіңіз (желательно до 20): ");
        int a = int.Parse(Console.ReadLine());
        Console.Write("Алаң енін ендігіңіз (желательно до 100): ");
        int b = int.Parse(Console.ReadLine());
        Console.WindowHeight = a;
        Console.WindowWidth = b;
        Console.BufferHeight = a;
        Console.BufferWidth = b;
        Console.Clear();
        var keyPressObservable = Observable.Create<ConsoleKeyInfo>(async (observer, cancellationToken) =>
        // Клавища басуды бақыланатын обьект ретінде алады
        {
            while (!cancellationToken.IsCancellationRequested) // Клавищті басуды тоқтатпай бақылап, обработчикке жіберу
            {
                var keyInfo = await Task.Run(() => Console.ReadKey(true), cancellationToken);
                observer.OnNext(keyInfo);
            }
        })
        // Тек бағыттарды көрсететін клавищтарды фильтрлеу
            .Where(keyInfo =>
                keyInfo.Key == ConsoleKey.UpArrow ||
                keyInfo.Key == ConsoleKey.DownArrow ||
                keyInfo.Key == ConsoleKey.LeftArrow ||
                keyInfo.Key == ConsoleKey.RightArrow)
               // клавищаны алу
            .Select(keyInfo => keyInfo.Key)
               // Әрбір 50 миллисекундта беру
            .Sample(TimeSpan.FromMilliseconds(50))
               // Қайта басылған клавищаларды жоқ қылу
            .DistinctUntilChanged();



        while (true) // Ойынды қайта бастай алу үшін циклда түр
        {

            SnakeGame snakeGame = new SnakeGame();

            // Подписка жасап блоктағы код орындау, код осындалып болған соң подписка жойылады
            using (keyPressObservable.Subscribe(snakeGame.OnKeyPress))
            {
                snakeGame.StartGame();
            }
            Console.SetCursorPosition(Convert.ToInt16(Console.WindowWidth * 0.2), 5);
            Console.Write("Заново:(z)");
            ConsoleKeyInfo k=Console.ReadKey(intercept:true); // z басылса ойын қайтадан басталады
            if (k.Key == ConsoleKey.Z)
                continue;
            else
                break;

        }


    }

}

class SnakeGame
{
    private int snakeX;
    private int snakeY;
    private List<(int,int)> telo = new List<(int,int)>();
    private int foodX;
    private int foodY;

    int biik = Console.WindowHeight;
    int eni = Console.WindowWidth;
    int tick_san = 150; // ойын жылдамдығы

    private bool isGameOver;

    private delegate (int,int) Direction(int x,int y);
    private Direction direction;

    // әрбәр бағыттарды белгілейтін делегаттар
    private Direction right = (int x, int y) => (x + 1, y);
    private Direction left = (int x, int y) => (x - 1, y);
    private Direction up = (int x, int y) => (x, y-1);
    private Direction down = (int x, int y) => (x, y+1);




    private readonly Random random = new Random();
    private readonly Subject<Unit> gameTickSubject = new Subject<Unit>(); // Бос бақыланатын обьект

    public SnakeGame() // Конструктор
    {
        snakeX = Console.WindowWidth / 2;
        snakeY = Console.WindowHeight / 2;

        telo.Add((snakeX, snakeY));
        telo.Add((snakeX-1, snakeY));
        direction = right;
        isGameOver = false;
    }

    public void StartGame() // Ойынды бастау
    {
        Console.Clear();
        DrawArea(); // алаңды салу
        DrawSnake(); 
        DrawFood();

        gameTickSubject // Ойын тоқтамайынша әрбір тикте UpdateGame() обработчик функциясын шақырып отырады
            .TakeWhile(_ => !isGameOver)
            .Subscribe(_ => UpdateGame());

        Task tick = Task.Factory.StartNew(() => // Бөлек ағында tick_san миллисекунд аралықпен тик жіберіп отырады
        {
            while (!isGameOver) 
            {
                gameTickSubject.OnNext(Unit.Default);
                Thread.Sleep(tick_san);
            }
        });

        while (!isGameOver) // ойын аяқталғанша кнопка басылғанын күтіп тұрады 
        {
            if (Console.KeyAvailable)
            {
                Console.ReadKey(intercept: true);
            }
        }

        Console.Clear(); // Ойын аяқталғанда консоль тазалайды

    }

    /// <summary>
    /// Кнопка басылғанда орындалып, бағытты көрсететін басты делегатты ауыстырады
    /// </summary>
    /// <param name="key">Басылаған кнопка келеді</param>
    public void OnKeyPress(ConsoleKey key)
    {
        if (isGameOver)
            return;
        switch (key)
        {
            case ConsoleKey.UpArrow:
                if (direction != down)
                    direction = up;
                break;
            case ConsoleKey.DownArrow:
                if (direction != up)
                    direction = down;
                break;
            case ConsoleKey.LeftArrow:
                if (direction != right)
                    direction = left;
                break;
            case ConsoleKey.RightArrow:
                if (direction != left)
                    direction = right;
                break;
        }
    }

    /// <summary>
    /// Каждый тик сайын ойынның состояниесін жаңартып отырады
    /// </summary>
    /// 
    private void UpdateGame()
    {
        MoveSnake();
        ProstoFood();
    }

    /// <summary>
    /// Жыланның бастапқы жүруін шақырады, алма жегенін тексеретін функцияны шақырады, 
    /// жылан соғысып қалмадыма деген функция шақырады
    /// </summary>
    private void MoveSnake()
    {
        (int,int) posl =  DrawSnake();

        if (snakeX == foodX && snakeY == foodY)
            EatFood(posl);

        IfEndGame();

    }

    /// <summary>
    /// Алаңды берілген размері бойынша сызып береді
    /// </summary>
    private void DrawArea()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        for (int i = 0; i < biik; i++)
        {
            for (int j = 0; j < eni; j++)
            {
                if (i == 0 || j == 0 || i == biik - 1 || j == eni - 1)
                    Console.Write("*");
                else
                    Console.Write(" ");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Жыланның басын делегаттағы бағыт бойынша қозғап, артындағы денесін соның артынан жүргізеді
    /// </summary>
    /// <returns>
    /// Жыланның артынан соңғы бос қалған ячейка координатасын қайтарады 
    /// (х координатасы,у координатасы)
    /// </returns>
    private (int,int) DrawSnake()
    {
        (int, int) posl = (snakeX, snakeY);
        (int, int) value = direction(snakeX, snakeY);
        snakeX = value.Item1;
        snakeY = value.Item2;
        for (int i = 0; i < telo.Count; i++)
        {
            (int, int) buff = telo[i];
            telo[i] = posl;
            posl = buff;
        }
        Console.ForegroundColor= ConsoleColor.DarkGreen;
        for (int i = 0; i < telo.Count; i++)
        {


            if (i == 0)
            {
                Console.SetCursorPosition(telo[i].Item1, telo[i].Item2);
                Console.Write("■");
            }
            else
            {
                Console.SetCursorPosition(telo[i].Item1, telo[i].Item2);
                Console.Write("•");
            }
            if (i == telo.Count - 1)
            {
                Console.SetCursorPosition(posl.Item1, posl.Item2);
                Console.Write(" ");
            }
        }

        return posl;
    }

    /// <summary>
    /// Жыланның өз денесімен немесе алаң қабырғаларымен соғысып қалғанын тексереді, 
    /// Егер соғысып қалса End() әдісін шақырады
    /// </summary>
    private void IfEndGame()
    {
        if (snakeX > eni - 2 || snakeX < 1 || snakeY > biik - 2 || snakeY < 1)
        {
            End();
        }
        for (int i = 1;i < telo.Count - 1; i++)
        {
            if (telo[0] == telo[i])
            {
                End();
                break;
            }
        }
    }

    /// <summary>
    /// Ойынды аяқтап, аяқталу туралы хабар шығарады
    /// </summary>
    private void End()
    {
        isGameOver = true;
        Console.Clear();
        Console.SetCursorPosition(Convert.ToInt16(eni * 0.2), 4);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Вот и всё моя жизнь!");
    }

    /// <summary>
    /// Тамақтың координатасын беріп, сол координатаға орнатады. Жылан бұрынғы тамақты жегенде шақырылады
    /// </summary>
    private void DrawFood()
    {
        foodX = random.Next(2, eni-2);
        foodY = random.Next(2, biik-2);
        Console.SetCursorPosition(foodX, foodY);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("0");
    }

    /// <summary>
    /// Тамақты әр тик сайын өз координатасында көрсетіп тұратын әдіс. 
    /// Тамақ жыланның үстінде пайда болып қалғанда жоғалып кетпес үшін.
    /// </summary>
    private void ProstoFood()
    {
        Console.SetCursorPosition(foodX, foodY);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("0");
    }

    /// <summary>
    /// Жылан тамак жегенде ұзартып, жаңа тамақ шығару
    /// </summary>
    /// <param name="posl"> Жылан денесін ұзарту үшін соңғы ячейкасынның координатасын алып, сол орында қосады </param>
    private void EatFood( (int,int) posl)
    {
        telo.Add((posl.Item1, posl.Item2));
        tick_san = tick_san - 3;
        DrawFood();
    }

}

