//
//  LanguageShortcut.swift
//
//
//  Created by Travis Prescott on 7/21/20.
//

import Foundation

protocol LanguageShortcut {
    var language: Languages { get set }

    var name: String { get set }

    var description: String { get set }

    var summary: String? { get set }

    var serializedName: String? { get set }

    var namespace: String? { get set }
}

extension LanguageShortcut {
    var name: String {
        get {
            return language.swift.name.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language.swift.name = newValue
        }
    }

    var description: String {
        get {
            return language.swift.description.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language.swift.description = newValue
        }
    }

    var summary: String? {
        get {
            return language.swift.summary?.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language.swift.summary = newValue
        }
    }

    var serializedName: String? {
        get {
            return language.swift.serializedName?.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language.swift.serializedName = newValue
        }
    }

    var namespace: String? {
        get {
            return language.swift.namespace?.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language.swift.namespace = newValue
        }
    }
}

protocol OptionalLanguageShortcut {
    var language: Languages? { get set }

    var name: String? { get set }

    var description: String? { get set }

    var summary: String? { get set }

    var serializedName: String? { get set }

    var namespace: String? { get set }
}

extension OptionalLanguageShortcut {
    var name: String? {
        get {
            return language?.swift.name.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            if let val = newValue {
                language?.swift.name = val
            }
        }
    }

    var description: String? {
        get {
            return language?.swift.description.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            if let val = newValue {
                language?.swift.description = val
            }
        }
    }

    var summary: String? {
        get {
            return language?.swift.summary?.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language?.swift.summary = newValue
        }
    }

    var serializedName: String? {
        get {
            return language?.swift.serializedName?.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language?.swift.serializedName = newValue
        }
    }

    var namespace: String? {
        get {
            return language?.swift.namespace?.trimmingCharacters(in: .whitespacesAndNewlines)
        }
        set {
            language?.swift.namespace = newValue
        }
    }
}
