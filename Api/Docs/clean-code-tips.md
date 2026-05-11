# 12 Clean Code Tips

> Source : Keivan Damirchi — adapté pour projets .NET/C#

---

## 1. Small Functions

Une fonction = une responsabilité. Si tu dois mettre un commentaire pour expliquer ce que fait une section, c'est qu'elle mérite sa propre méthode.

```csharp
// ❌
void ProcessOrder(Order order)
{
    // validate order
    // process payment
}

// ✅
void ValidateOrder(Order order) { ... }
void ProcessPayment(Order order) { ... }
```

---

## 2. Minimize Dependencies

Injecte les dépendances plutôt que de les instancier en dur.

```csharp
// ❌
class UserService
{
    UserRepository userRepository = new UserRepository();
}

// ✅
class UserService
{
    private readonly IUserRepository _userRepository;
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
}
```

---

## 3. Readability

Préfère des noms explicites aux variables jetables.

```csharp
// ❌
int i = 0;
for (int j = 0; j < 10; j++)
    i += j;

// ✅
int sum = 0;
for (int num = 0; num < 10; num++)
    sum += num;
```

---

## 4. Avoid Boolean Parameters

Les paramètres booléens cachent l'intention. Crée deux méthodes distinctes.

```csharp
// ❌
void SetUserStatus(bool isActive) { ... }

// ✅
void ActivateUser() { }
void DeactivateUser() { }
```

---

## 5. Error Handling

Capture les exceptions spécifiques, pas `Exception` générique.

```csharp
// ❌
try { }
catch (Exception e) { e.PrintStackTrace(); }

// ✅
try { }
catch (FileNotFoundException e) { /* handle missing file */ }
catch (IOException e) { /* handle IO issues */ }
```

---

## 6. Consistent Formatting

Applique un style de formatage cohérent dans tout le projet. Utilise `.editorconfig` ou StyleCop pour l'automatiser.

```csharp
// ❌
public class Example{ public void Foo(){...}}

// ✅
public class Example
{
    public void Foo() { ... }
}
```

---

## 7. Comment Wisely

Commente le *pourquoi*, pas le *quoi*. Le code s'explique lui-même.

```csharp
// ❌
// Increment i by 1
i += 1;

// ✅
// Adjust index to account for 1-based array
index += 1;
```

---

## 8. Meaningful Names

Un nom de variable doit se lire comme de la prose.

```csharp
// ❌
int d; // elapsed time in days

// ✅
int elapsedTimeInDays;
```

---

## 9. Single Responsibility

Une classe = une responsabilité. Découpe `OrderManager` en classes spécialisées.

```csharp
// ❌
class OrderManager
{
    void ProcessOrder(Order order) { ... }
    void SaveOrder(Order order) { ... }
}

// ✅
class OrderProcessor { void Process(Order order) { ... } }
class OrderRepository { void Save(Order order) { ... } }
```

---

## 10. Avoid Magic Numbers

Remplace les constantes littérales par des constantes nommées ou des enums.

```csharp
// ❌
if (user.Age > 65) { ... }

// ✅
const int RetirementAge = 65;
if (user.Age > RetirementAge) { ... }
// Ou mieux : utilise un enum
```

---

## 11. Encapsulate Conditionals

Extrait les conditions complexes dans des méthodes dédiées.

```csharp
// ❌
if (employee.Age > RetirementAge) { ... }

// ✅
if (employee.IsEligibleForRetirement()) { ... }
```

---

## 12. Use Exceptions, Not Error Codes

Ne retourne pas `-1` ou `0` pour signaler une erreur. Lance une exception explicite.

```csharp
// ❌
if (user.IsValid())
    return 0; // success
else
    return -1; // error

// ✅
if (!user.IsValid())
    throw new InvalidUserException();
```
